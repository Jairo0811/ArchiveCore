using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ArchiveCore.Api.Tests;

/// <summary>Host without a reachable database: exercises routing, auth and error contracts only.</summary>
public sealed class ArchiveCoreFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Never contacted by these tests (port 1 is closed); required to build the DbContext.
                ["ConnectionStrings:ArchiveCore"] = "Server=127.0.0.1,1;Database=ArchiveCoreDb;User Id=none;Password=none;Connect Timeout=1;TrustServerCertificate=True",
                ["Jwt:SigningKey"] = TestSecrets.NewSigningKey(),
                ["BootstrapAdmin:Enabled"] = "false",
                ["Security:UseHttpsRedirection"] = "false"
            });
        });
    }
}

public sealed class ApiSmokeTests(ArchiveCoreFactory factory) : IClassFixture<ArchiveCoreFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Root_returns_running_service()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();

        Assert.Contains("ArchiveCore.WebApi", payload);
        Assert.Contains("running", payload);
    }

    [Theory]
    [InlineData("GET", "/api/v1/records")]
    [InlineData("GET", "/api/v1/dashboard")]
    [InlineData("GET", "/api/v1/audit")]
    [InlineData("GET", "/api/v1/users")]
    [InlineData("GET", "/api/v1/academic/sql-lab/queries")]
    [InlineData("POST", "/api/v1/records")]
    [InlineData("GET", "/api/v1/documents/1/download")]
    public async Task Protected_endpoints_return_401_problem_without_token(string method, string path)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Invalid_bearer_token_is_rejected()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/records");
        request.Headers.Authorization = new("Bearer", "not-a-jwt");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_json_returns_400_problem_details()
    {
        var response = await _client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("   at ", body); // no stack trace
    }

    [Fact]
    public async Task Login_with_empty_fields_returns_400_validation_problem()
    {
        var response = await _client.PostAsync(
            "/api/v1/auth/login",
            new StringContent("""{"email":"","password":""}""", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errors\"", body);
        Assert.Contains("email", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Api_responses_carry_security_headers_and_correlation_id()
    {
        var response = await _client.GetAsync("/api/v1/records");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task Cors_does_not_allow_arbitrary_origins()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}

internal static class TestSecrets
{
    public static string NewSigningKey() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));

    /// <summary>Random password satisfying the policy; generated per run, never committed.</summary>
    public static string NewPassword() =>
        "Ac1!" + Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(18))
            .Replace('+', 'x').Replace('/', 'y');
}
