namespace ArchiveCore.WebApi.Http;

/// <summary>
/// Assigns a GUID correlation id per request (honouring a valid incoming X-Correlation-Id),
/// echoes it in the response and exposes it to logging and SQL Server audit.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    private const string ItemKey = "ArchiveCore.CorrelationId";

    public static Guid? Get(HttpContext context) =>
        context.Items.TryGetValue(ItemKey, out var value) && value is Guid id ? id : null;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId =
            context.Request.Headers.TryGetValue(HeaderName, out var header) &&
            Guid.TryParse(header.ToString(), out var incoming) &&
            incoming != Guid.Empty
                ? incoming
                : Guid.NewGuid();

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId.ToString();
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
