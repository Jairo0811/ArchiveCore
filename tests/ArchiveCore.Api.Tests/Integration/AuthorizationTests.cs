using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthorizationTests(IntegrationContext ctx)
{
    public static TheoryData<string, string> AdministratorOnly => new()
    {
        { "GET", "/api/v1/audit" },
        { "GET", "/api/v1/users" },
        { "GET", "/api/v1/users/roles" },
        { "POST", "/api/v1/users" },
        { "GET", "/api/v1/academic/sql-lab/queries" },
        { "GET", "/api/v1/academic/sql-lab/queries/legacy-admins" }
    };

    public static TheoryData<string, string> ManagerMutations => new()
    {
        { "POST", "/api/v1/records" },
        { "PUT", "/api/v1/records/1" },
        { "POST", "/api/v1/records/1/workflow/transfer" },
        { "POST", "/api/v1/records/1/workflow/close" },
        { "POST", "/api/v1/documents" },
        { "POST", "/api/v1/documents/1/versions" },
        { "DELETE", "/api/v1/documents/1" }
    };

    [Theory]
    [MemberData(nameof(AdministratorOnly))]
    public async Task RecordsManager_and_User_get_403_on_administrator_endpoints(string method, string path)
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(ctx.ManagerA, method, path)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(ctx.Reader, method, path)).StatusCode);
    }

    [Theory]
    [MemberData(nameof(ManagerMutations))]
    public async Task User_role_gets_403_on_mutations(string method, string path)
    {
        var response = await Send(ctx.Reader, method, path);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/v1/records")]
    [InlineData("/api/v1/dashboard")]
    [InlineData("/api/v1/catalogs/record-statuses")]
    [InlineData("/api/v1/catalogs/document-categories")]
    [InlineData("/api/v1/catalogs/users")]
    [InlineData("/api/v1/account/me")]
    public async Task User_role_has_read_access(string path)
    {
        var response = await ctx.Reader.Client.GetAsync(path);
        await response.EnsureStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Administrator_reaches_administrator_endpoints()
    {
        await (await ctx.Admin.Client.GetAsync("/api/v1/audit")).EnsureStatusAsync(HttpStatusCode.OK);
        await (await ctx.Admin.Client.GetAsync("/api/v1/users")).EnsureStatusAsync(HttpStatusCode.OK);
        await (await ctx.Admin.Client.GetAsync("/api/v1/academic/sql-lab/queries")).EnsureStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RecordsManager_can_manage_records_but_cannot_create_users()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        Assert.True(record.RecordId > 0);

        var createAdmin = await ctx.ManagerA.Client.PostAsJsonAsync("/api/v1/users", new
        {
            firstName = "Intento",
            lastName = "Escalada",
            email = "escalada@archivecore.test",
            password = TestSecrets.NewPassword(),
            roles = new[] { "Administrator" }
        }, IntegrationContext.Json);

        Assert.Equal(HttpStatusCode.Forbidden, createAdmin.StatusCode);
    }

    [Fact]
    public async Task Catalog_user_lookup_exposes_no_contact_data()
    {
        var body = await ctx.Reader.Client.GetStringAsync("/api/v1/catalogs/users");
        Assert.DoesNotContain("@", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    private static Task<HttpResponseMessage> Send(TestUser user, string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
            request.Content = JsonContent.Create(new { }, options: IntegrationContext.Json);
        return user.Client.SendAsync(request);
    }
}
