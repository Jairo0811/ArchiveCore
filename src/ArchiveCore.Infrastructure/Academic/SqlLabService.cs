using System.Data;
using ArchiveCore.Application.Academic;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Academic;

public sealed class SqlLabService(ArchiveCoreDbContext db) : ISqlLabService
{
    private static readonly SqlLabQueryDefinition[] Queries =
    [
        new(
            "legacy-admins",
            "Administradores — consulta original",
            "SELECT * FROM Administrador",
            """
            SELECT
                u.UserId AS IdAdmin,
                u.FirstName AS Nombre,
                u.LastName AS Apellido,
                u.Email AS Correo,
                u.IsActive AS Activo
            FROM dbo.Users u
            INNER JOIN dbo.UserRoles ur ON ur.UserId = u.UserId
            INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId
            WHERE r.Name = N'Administrator'
            ORDER BY u.UserId;
            """,
            "La tabla Administrador del proyecto original evolucionó a Users + Roles + UserRoles. Esta consulta recupera los usuarios que actualmente poseen el rol Administrator.",
            "docs/legacy/DatabaseProject-original.sql"
        ),
        new(
            "legacy-users",
            "Usuarios — consulta original",
            "SELECT * FROM Usuarios",
            """
            SELECT
                u.UserId AS IdUser,
                u.FirstName AS Nombre,
                u.LastName AS Apellido,
                u.Email AS Correo,
                u.BirthDate AS FechaNacimiento,
                u.IsActive AS Activo,
                u.CreatedAtUtc AS CreadoUtc
            FROM dbo.Users u
            ORDER BY u.UserId;
            """,
            "La tabla Usuarios original se consolidó en dbo.Users. El modelo moderno elimina duplicación de identidad y separa los roles mediante relaciones normalizadas.",
            "docs/legacy/DatabaseProject-original.sql"
        )
    ];

    public IReadOnlyCollection<SqlLabQueryDefinition> GetQueries() => Queries;

    public async Task<SqlLabQueryResult?> ExecuteAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var query = Queries.FirstOrDefault(
            x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

        if (query is null)
            return null;

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = query.ModernSql;
            command.CommandType = CommandType.Text;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columns = Enumerable.Range(0, reader.FieldCount)
                .Select(reader.GetName)
                .ToArray();

            var rows = new List<IReadOnlyDictionary<string, object?>>();

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                for (var index = 0; index < reader.FieldCount; index++)
                {
                    row[reader.GetName(index)] =
                        await reader.IsDBNullAsync(index, cancellationToken)
                            ? null
                            : reader.GetValue(index);
                }

                rows.Add(row);
            }

            return new SqlLabQueryResult(query, columns, rows);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
