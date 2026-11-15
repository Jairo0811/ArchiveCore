using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ArchiveCore.Infrastructure.Persistence;

/// <summary>Executes ArchiveCore stored procedures on the DbContext connection.</summary>
internal static class StoredProcedures
{
    /// <summary>
    /// Executes a procedure and returns the first column of the first row
    /// (the procedures return the affected row with its identity first).
    /// </summary>
    public static async Task<T> ExecuteScalarAsync<T>(
        ArchiveCoreDbContext db,
        string procedure,
        IReadOnlyList<SqlParameter> parameters,
        CancellationToken cancellationToken)
    {
        // Opening through EF fires AuditSessionContextInterceptor (SESSION_CONTEXT).
        await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using DbCommand command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

            foreach (var parameter in parameters)
                command.Parameters.Add(parameter);

            var result = await SqlErrorTranslator.RunAsync(() => command.ExecuteScalarAsync(cancellationToken));

            if (result is null or DBNull)
                throw new InvalidOperationException($"Procedure {procedure} did not return a result.");

            return (T)Convert.ChangeType(result, typeof(T));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public static SqlParameter Param(string name, SqlDbType type, object? value, int size = 0)
    {
        var parameter = new SqlParameter(name, type) { Value = value ?? DBNull.Value };
        if (size > 0)
            parameter.Size = size;
        return parameter;
    }
}
