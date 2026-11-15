namespace ArchiveCore.Application.Common;

/// <summary>
/// Base type for expected, client-facing application errors. Messages are safe to return
/// to API consumers and never contain SQL, stack traces or file-system details.
/// </summary>
public abstract class ApplicationErrorException(string message) : Exception(message);

/// <summary>Input failed validation (HTTP 400).</summary>
public sealed class RequestValidationException : ApplicationErrorException
{
    public RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("La solicitud contiene datos no válidos.")
    {
        Errors = errors;
    }

    public RequestValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = [error] })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

/// <summary>The requested resource does not exist (HTTP 404).</summary>
public sealed class ResourceNotFoundException(string message) : ApplicationErrorException(message);

/// <summary>The request conflicts with the current state, a duplicate or a concurrent change (HTTP 409).</summary>
public sealed class ConflictException(string message) : ApplicationErrorException(message);

/// <summary>The authenticated actor is not allowed to perform the operation (HTTP 403).</summary>
public sealed class ForbiddenOperationException(string message) : ApplicationErrorException(message);

/// <summary>Physical document storage failed (HTTP 500 with a safe message).</summary>
public sealed class StorageFailureException(string message, Exception? inner = null)
    : Exception(message, inner);
