using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

/// <summary>
/// Real SQL Server (Testcontainers) initialized with the repository scripts in the documented
/// order. Triggers, SESSION_CONTEXT, filtered indexes, procedures and rowversion behave exactly
/// as in production; nothing is replaced by an in-memory or SQLite provider.
/// </summary>
public sealed partial class SqlServerFixture : IAsyncLifetime
{
    public const string Image = "mcr.microsoft.com/mssql/server:2022-latest";

    public static readonly string[] ScriptOrder =
    [
        "01-schema.sql",
        "02-constraints.sql",
        "07-seed.sql",
        "03-indexes.sql",
        "04-views.sql",
        "05-procedures.sql",
        "06-triggers.sql",
        "09-auth.sql",
        "10-validation.sql"
    ];

    private readonly MsSqlContainer _container = new MsSqlBuilder(Image).Build();

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Messages printed by 10-validation.sql during initialization.</summary>
    public string ValidationOutput { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var master = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "master",
            TrustServerCertificate = true
        };

        var messages = new StringBuilder();
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            connection.InfoMessage += (_, e) => messages.AppendLine(e.Message);
            await connection.OpenAsync();

            foreach (var script in ScriptOrder)
            {
                var path = Path.Combine(AppContext.BaseDirectory, "database", script);
                foreach (var batch in SplitBatches(await File.ReadAllTextAsync(path)))
                {
                    await using var command = new SqlCommand(batch, connection) { CommandTimeout = 120 };
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                    catch (SqlException ex)
                    {
                        throw new InvalidOperationException($"Script {script} failed: {ex.Message}", ex);
                    }
                }
            }
        }

        ValidationOutput = messages.ToString();

        ConnectionString = new SqlConnectionStringBuilder(master.ConnectionString)
        {
            InitialCatalog = "ArchiveCoreDb"
        }.ConnectionString;
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public static IEnumerable<string> SplitBatches(string script) =>
        GoSeparator().Split(script).Where(batch => !string.IsNullOrWhiteSpace(batch));

    [GeneratedRegex(@"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoSeparator();
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationContext>
{
    public const string Name = "SQL Server integration";
}
