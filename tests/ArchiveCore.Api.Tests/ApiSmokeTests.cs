using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ArchiveCore.Api.Tests;

public sealed class ArchiveCoreFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "archivecore-tests-signing-key-at-least-32-characters",
                ["BootstrapAdmin:Enabled"] = "false"
            });
        });
    }
}

public sealed class ApiSmokeTests : IClassFixture<ArchiveCoreFactory>
{
    private readonly HttpClient _client;

    public ApiSmokeTests(ArchiveCoreFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Root_returns_running_service()
    {
        var response = await _client.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync();

        Assert.Contains("ArchiveCore.WebApi", payload);
        Assert.Contains("running", payload);
    }
}
