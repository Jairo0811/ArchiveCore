using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class SqlLabAndDatabaseTests(IntegrationContext ctx)
{
    [Fact]
    public void Validation_script_passes_on_a_clean_database() =>
        Assert.Contains("ArchiveCore database validation passed.", ctx.Sql.ValidationOutput);

    [Fact]
    public async Task Sql_lab_lists_predefined_queries_without_legacy_inserts()
    {
        var json = await ctx.Admin.Client.GetStringAsync("/api/v1/academic/sql-lab/queries");

        Assert.Contains("SELECT * FROM Administrador", json);
        Assert.Contains("SELECT * FROM Usuarios", json);
        Assert.DoesNotContain("INSERT", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sql_lab_administrators_query_joins_users_roles_and_user_roles()
    {
        var response = await ctx.Admin.Client.GetAsync("/api/v1/academic/sql-lab/queries/legacy-admins");
        await response.EnsureStatusAsync(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var modernSql = doc.RootElement.GetProperty("query").GetProperty("modernSql").GetString()!;
        Assert.Contains("dbo.UserRoles", modernSql);
        Assert.Contains("dbo.Roles", modernSql);

        var ids = doc.RootElement.GetProperty("rows").EnumerateArray()
            .Select(r => r.GetProperty("IdAdmin").GetInt32()).ToArray();
        Assert.Contains(ctx.Admin.UserId, ids);
        Assert.DoesNotContain(ctx.ManagerA.UserId, ids);

        var users = await ctx.Admin.Client.GetAsync("/api/v1/academic/sql-lab/queries/legacy-users");
        await users.EnsureStatusAsync(HttpStatusCode.OK);
        Assert.DoesNotContain("dbo.Roles", await users.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("SELECT 1")]
    [InlineData("legacy-admins; DROP TABLE dbo.Users")]
    public async Task Sql_lab_never_executes_arbitrary_keys(string key)
    {
        var response = await ctx.Admin.Client.GetAsync($"/api/v1/academic/sql-lab/queries/{Uri.EscapeDataString(key)}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Close_procedure_refuses_to_close_twice()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();

        await using var connection = new SqlConnection(ctx.Sql.ConnectionString);
        await connection.OpenAsync();

        async Task CloseAsync()
        {
            await using var command = new SqlCommand("dbo.sp_CloseRecord", connection) { CommandType = System.Data.CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@RecordId", record.RecordId);
            command.Parameters.AddWithValue("@PerformedByUserId", ctx.ManagerA.UserId);
            await command.ExecuteNonQueryAsync();
        }

        await CloseAsync();
        var error = await Assert.ThrowsAsync<SqlException>(CloseAsync);
        Assert.Equal(50013, error.Number);
    }

    [Fact]
    public async Task Filtered_unique_index_allows_only_one_current_version()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);
        await (await ctx.ManagerA.UploadAsync(document.DocumentId, ApiFlow.PdfBytes, "unica.pdf")).EnsureStatusAsync(HttpStatusCode.OK);

        await using var connection = new SqlConnection(ctx.Sql.ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("""
            INSERT INTO dbo.DocumentVersions
            (DocumentId, VersionNumber, OriginalFileName, StoredFileName, StoragePath, FileSizeBytes, Sha256Hash, UploadedByUserId, IsCurrent)
            VALUES (@DocumentId, 99, N'x.pdf', N'x.pdf', N'x/x.pdf', 1, REPLICATE('A', 64), @UserId, 1);
            """, connection);
        command.Parameters.AddWithValue("@DocumentId", document.DocumentId);
        command.Parameters.AddWithValue("@UserId", ctx.ManagerA.UserId);

        var error = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal(2601, error.Number);
    }
}

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserAdministrationTests(IntegrationContext ctx)
{
    [Fact]
    public async Task Administrator_lists_users_with_roles()
    {
        var response = await ctx.Admin.Client.GetAsync("/api/v1/users");
        await response.EnsureStatusAsync(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(ctx.ManagerA.Email, body);
        Assert.Contains("RecordsManager", body);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Duplicate_email_weak_password_and_unknown_roles_are_rejected()
    {
        var duplicate = await ctx.Admin.Client.PostAsJsonAsync("/api/v1/users", new
        {
            firstName = "Copia", lastName = "Duplicada", email = ctx.ManagerA.Email.ToUpperInvariant(),
            password = TestSecrets.NewPassword(), roles = new[] { "User" }
        }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var weak = await ctx.Admin.Client.PostAsJsonAsync("/api/v1/users", new
        {
            firstName = "Clave", lastName = "Débil", email = $"weak.{Guid.NewGuid():N}@archivecore.test",
            password = "12345678", roles = new[] { "User" }
        }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);
        Assert.Contains("password", await weak.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var role = await ctx.Admin.Client.PostAsJsonAsync("/api/v1/users", new
        {
            firstName = "Rol", lastName = "Falso", email = $"role.{Guid.NewGuid():N}@archivecore.test",
            password = TestSecrets.NewPassword(), roles = new[] { "SuperAdmin" }
        }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.BadRequest, role.StatusCode);
    }

    [Fact]
    public async Task Administrator_cannot_deactivate_own_account()
    {
        var response = await ctx.Admin.Client.PatchAsJsonAsync(
            $"/api/v1/users/{ctx.Admin.UserId}/status", new { isActive = false }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
