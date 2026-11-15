using System.Data;
using System.Data.Common;
using ArchiveCore.Application.Auth;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ArchiveCore.Infrastructure.Persistence;

/// <summary>
/// Propagates the authenticated actor, client IP and correlation id to SQL Server through
/// <c>SESSION_CONTEXT</c> every time EF Core opens a (pooled) connection, so the audit
/// triggers attribute each change to the user who actually performed it.
/// </summary>
/// <remarks>
/// All three keys are written on every open — including explicit NULLs — so no value can
/// leak from a previous request that used the same pooled physical connection.
/// </remarks>
public sealed class AuditSessionContextInterceptor(ICurrentUser currentUser) : DbConnectionInterceptor
{
    internal const string CommandText =
        "EXEC sys.sp_set_session_context @key = N'UserId', @value = @UserId; " +
        "EXEC sys.sp_set_session_context @key = N'IpAddress', @value = @IpAddress; " +
        "EXEC sys.sp_set_session_context @key = N'CorrelationId', @value = @CorrelationId;";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = CreateCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = CommandText;
        command.CommandType = CommandType.Text;

        var userId = currentUser.IsAuthenticated ? currentUser.UserId : null;
        var ip = Truncate(currentUser.IpAddress, 45);

        command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Int)
        {
            Value = (object?)userId ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@IpAddress", SqlDbType.VarChar, 45)
        {
            Value = (object?)ip ?? DBNull.Value
        });
        command.Parameters.Add(new SqlParameter("@CorrelationId", SqlDbType.UniqueIdentifier)
        {
            Value = (object?)currentUser.CorrelationId ?? DBNull.Value
        });

        return command;
    }

    private static string? Truncate(string? value, int max) =>
        value is null ? null : value.Length <= max ? value : value[..max];
}
