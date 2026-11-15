namespace ArchiveCore.WebApi.Http;

/// <summary>Conservative security headers for a JSON/file API.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            // The API never serves HTML documents; lock it down completely.
            if (!context.Request.Path.StartsWithSegments("/openapi"))
                headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            if (context.Request.Path.StartsWithSegments("/api"))
                headers.CacheControl = "no-store";

            return Task.CompletedTask;
        });

        return next(context);
    }
}
