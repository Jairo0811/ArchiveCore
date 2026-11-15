using ArchiveCore.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.WebApi.Http;

/// <summary>
/// Maps exceptions to RFC 7807 ProblemDetails with semantic status codes.
/// Unexpected errors return a generic 500: no stack traces, SQL messages or paths are exposed.
/// </summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            RequestValidationException validation => new HttpValidationProblemDetails(
                validation.Errors.ToDictionary(x => x.Key, x => x.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos no válidos",
                Detail = validation.Message
            },
            ResourceNotFoundException notFound => Problem(StatusCodes.Status404NotFound, "Recurso no encontrado", notFound.Message),
            ConflictException conflict => Problem(StatusCodes.Status409Conflict, "Conflicto", conflict.Message),
            ForbiddenOperationException forbidden => Problem(StatusCodes.Status403Forbidden, "Operación no permitida", forbidden.Message),
            DbUpdateConcurrencyException => Problem(StatusCodes.Status409Conflict, "Conflicto de concurrencia",
                "El recurso fue modificado por otro usuario. Recarga los datos e inténtalo de nuevo."),
            BadHttpRequestException badRequest => Problem(
                badRequest.StatusCode,
                badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge ? "Contenido demasiado grande" : "Solicitud no válida",
                badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge
                    ? "El contenido supera el tamaño máximo permitido."
                    : "El cuerpo o los parámetros de la solicitud no tienen un formato válido."),
            StorageFailureException storage => Problem(StatusCodes.Status500InternalServerError, "Error de almacenamiento", storage.Message),
            OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested =>
                Problem(499, "Solicitud cancelada", "El cliente canceló la solicitud."),
            _ => Problem(StatusCodes.Status500InternalServerError, "Error inesperado",
                "Ocurrió un error inesperado. Si persiste, contacta al administrador indicando el identificador de correlación.")
        };

        if (problem.Status >= 500)
            logger.LogError(exception, "Unhandled exception processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("Request rejected with {Status}: {ExceptionType}", problem.Status, exception.GetType().Name);

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail) =>
        new() { Status = status, Title = title, Detail = detail };
}
