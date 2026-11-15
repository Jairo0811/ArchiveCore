using ArchiveCore.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Persistence;

/// <summary>
/// Translates the stable THROW numbers raised by the ArchiveCore stored procedures (and
/// unique-key violations) into application exceptions with safe, Spanish client messages.
/// Unknown SQL errors are rethrown unchanged and end up as a generic HTTP 500.
/// </summary>
public static class SqlErrorTranslator
{
    public static Exception? Translate(Exception exception)
    {
        var sql = exception as SqlException
            ?? (exception as DbUpdateException)?.InnerException as SqlException;

        if (sql is null)
            return null;

        return sql.Number switch
        {
            50003 or 50005 => new ResourceNotFoundException("El expediente no existe."),
            50020 => new ResourceNotFoundException("El documento no existe o fue eliminado."),
            50010 => new ConflictException("Ya existe un expediente con ese código."),
            50011 => new RequestValidationException("UserId", "El usuario destino no existe o está inactivo."),
            50012 => new ForbiddenOperationException("Tu usuario no está activo."),
            50013 => new ConflictException("El expediente está cerrado y no admite esta operación."),
            50014 => new ConflictException("El expediente ya está asignado a ese usuario."),
            50021 => new ConflictException("El expediente del documento está cerrado; no admite nuevas versiones."),
            2601 or 2627 => new ConflictException("La operación genera un registro duplicado."),
            547 => new ConflictException("La operación viola una restricción de integridad."),
            _ => null
        };
    }

    public static async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (Translate(ex) is { } translated)
        {
            throw translated;
        }
    }

    public static async Task RunAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex) when (Translate(ex) is { } translated)
        {
            throw translated;
        }
    }
}
