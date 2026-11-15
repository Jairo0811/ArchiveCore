using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthTests(IntegrationContext ctx)
{
    [Fact]
    public async Task Health_reports_healthy_database()
    {
        var response = await ctx.NewClient().GetAsync("/health");
        await response.EnsureStatusAsync(HttpStatusCode.OK);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_returns_access_token_and_httponly_refresh_cookie_only()
    {
        var client = ctx.NewClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = ctx.ManagerA.Email, password = ctx.ManagerA.Password }, IntegrationContext.Json);

        await response.EnsureStatusAsync(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", body);
        Assert.DoesNotContain("refreshToken", body, StringComparison.OrdinalIgnoreCase);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("archivecore_refresh=", cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_credentials_return_401_problem()
    {
        var client = ctx.NewClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = ctx.ManagerA.Email, password = "Wrong-Password-123!" }, IntegrationContext.Json);
        var unknownUser = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "nobody@archivecore.test", password = "Wrong-Password-123!" }, IntegrationContext.Json);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal("application/problem+json", wrongPassword.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Refresh_rotates_the_cookie_and_rejects_the_previous_token()
    {
        var user = await ctx.LoginAsync(ctx.ManagerA.Email, ctx.ManagerA.Password);

        var first = await user.Client.PostAsync("/api/v1/auth/refresh", null);
        await first.EnsureStatusAsync(HttpStatusCode.OK);
        var rotated = await first.ReadAsync<AuthBody>();
        Assert.False(string.IsNullOrWhiteSpace(rotated.AccessToken));
        Assert.Equal(user.UserId, rotated.User.UserId);

        var oldCookieClient = ctx.NewClient();
        var login = await oldCookieClient.PostAsJsonAsync("/api/v1/auth/login",
            new { email = ctx.ManagerA.Email, password = ctx.ManagerA.Password }, IntegrationContext.Json);
        var originalCookie = login.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        var rotate = await oldCookieClient.PostAsync("/api/v1/auth/refresh", null);
        await rotate.EnsureStatusAsync(HttpStatusCode.OK);

        // Replaying the pre-rotation token must fail.
        var replay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        replay.Headers.Add("Cookie", originalCookie);
        var replayResponse = await ctx.Factory.CreateClient(new() { HandleCookies = false }).SendAsync(replay);
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_without_cookie_returns_401()
    {
        var response = await ctx.NewClient().PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Revoke_ends_the_session_and_audits_logout()
    {
        var user = await ctx.LoginAsync(ctx.ManagerB.Email, ctx.ManagerB.Password);

        var revoke = await user.Client.PostAsync("/api/v1/auth/revoke", null);
        await revoke.EnsureStatusAsync(HttpStatusCode.NoContent);

        var refresh = await user.Client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        var audit = await ctx.Admin.AuditAsync("Users", user.UserId.ToString());
        Assert.Contains(audit, x => x.ActionType == "LOGIN" && x.UserId == user.UserId);
        Assert.Contains(audit, x => x.ActionType == "LOGOUT" && x.UserId == user.UserId);
    }

    [Fact]
    public async Task Deactivated_user_cannot_login_or_refresh()
    {
        var temp = await ctx.CreateUserAsync("Temporal", "Inactivo", "User");

        var deactivate = await ctx.Admin.Client.PatchAsJsonAsync(
            $"/api/v1/users/{temp.UserId}/status", new { isActive = false }, IntegrationContext.Json);
        await deactivate.EnsureStatusAsync(HttpStatusCode.OK);

        var refresh = await temp.Client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);

        var login = await ctx.NewClient().PostAsJsonAsync("/api/v1/auth/login",
            new { email = temp.Email, password = temp.Password }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_is_rate_limited()
    {
        await using var limited = new IntegrationApiFactory(ctx.Sql, "unused@archivecore.test", TestSecrets.NewPassword(), ctx.StorageRoot);
        limited.Overrides["RateLimiting:LoginPerMinute"] = "2";
        var client = limited.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
            last = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { email = "nobody@archivecore.test", password = "Wrong-Password-123!" }, IntegrationContext.Json);

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.Equal("application/problem+json", last.Content.Headers.ContentType?.MediaType);
    }
}
