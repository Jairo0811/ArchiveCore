using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

public sealed class IntegrationApiFactory(SqlServerFixture sql, string adminEmail, string adminPassword, string storageRoot)
    : WebApplicationFactory<Program>
{
    public Dictionary<string, string?> Overrides { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:ArchiveCore"] = sql.ConnectionString,
                ["Jwt:SigningKey"] = TestSecrets.NewSigningKey(),
                ["Jwt:AccessTokenMinutes"] = "15",
                ["BootstrapAdmin:Enabled"] = "true",
                ["BootstrapAdmin:FirstName"] = "Admin",
                ["BootstrapAdmin:LastName"] = "Pruebas",
                ["BootstrapAdmin:Email"] = adminEmail,
                ["BootstrapAdmin:Password"] = adminPassword,
                ["DocumentStorage:RootPath"] = storageRoot,
                ["DocumentStorage:MaxFileSizeBytes"] = (2 * 1024 * 1024).ToString(),
                // The in-memory test server speaks plain HTTP.
                ["Auth:RefreshCookie:Secure"] = "false",
                ["Security:UseHttpsRedirection"] = "false",
                ["RateLimiting:LoginPerMinute"] = "1000",
                ["RateLimiting:RefreshPerMinute"] = "1000"
            };

            foreach (var (key, value) in Overrides)
                settings[key] = value;

            config.AddInMemoryCollection(settings);
        });
    }
}

/// <summary>
/// One API host over the shared SQL Server container plus the seeded test identities:
/// the bootstrap Administrator, two RecordsManagers (A and B) and a read-only User.
/// Every password is random per run.
/// </summary>
public sealed class IntegrationContext : IAsyncLifetime
{
    private readonly SqlServerFixture sql = new();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string StorageRoot { get; } = Path.Combine(Path.GetTempPath(), "archivecore-tests-" + Guid.NewGuid().ToString("N"));
    public TestUser Admin { get; private set; } = null!;
    public TestUser ManagerA { get; private set; } = null!;
    public TestUser ManagerB { get; private set; } = null!;
    public TestUser Reader { get; private set; } = null!;
    public IntegrationApiFactory Factory { get; private set; } = null!;
    public SqlServerFixture Sql => sql;

    public async Task InitializeAsync()
    {
        await sql.InitializeAsync();

        var adminPassword = TestSecrets.NewPassword();
        Factory = new IntegrationApiFactory(sql, "admin@archivecore.test", adminPassword, StorageRoot);

        Admin = await LoginAsync("admin@archivecore.test", adminPassword);
        ManagerA = await CreateUserAsync("Gestora", "Alfa", "RecordsManager");
        ManagerB = await CreateUserAsync("Gestor", "Beta", "RecordsManager");
        Reader = await CreateUserAsync("Lectora", "Gamma", "User");
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await sql.DisposeAsync();
        try { Directory.Delete(StorageRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    public HttpClient NewClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });

    public async Task<TestUser> CreateUserAsync(string firstName, string lastName, params string[] roles)
    {
        var email = $"{firstName}.{lastName}.{Guid.NewGuid():N}@archivecore.test".ToLowerInvariant();
        var password = TestSecrets.NewPassword();

        var response = await Admin.Client.PostAsJsonAsync("/api/v1/users", new
        {
            firstName,
            lastName,
            email,
            password,
            roles
        }, Json);

        await response.EnsureStatusAsync(HttpStatusCode.Created);
        return await LoginAsync(email, password);
    }

    public async Task<TestUser> LoginAsync(string email, string password)
    {
        var client = NewClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password }, Json);
        await response.EnsureStatusAsync(HttpStatusCode.OK);

        var auth = (await response.Content.ReadFromJsonAsync<AuthBody>(Json))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return new TestUser(auth.User.UserId, email, password, client, auth.AccessToken);
    }
}

public sealed record TestUser(int UserId, string Email, string Password, HttpClient Client, string AccessToken);

public sealed record AuthBody(string AccessToken, DateTime AccessTokenExpiresAtUtc, AuthUser User);

public sealed record AuthUser(int UserId, string FirstName, string LastName, string Email, string[] Roles);

public static class HttpAssertions
{
    public static async Task EnsureStatusAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Expected {(int)expected} {expected} but got {(int)response.StatusCode} {response.StatusCode} for " +
                $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: {body}");
        }
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(IntegrationContext.Json))!;
}
