using ArchiveCore.Application.Auditing;

namespace ArchiveCore.WebApi.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var audit = app.MapGroup("/api/v1/audit")
            .WithTags("Audit")
            .RequireAuthorization("AdministratorsOnly");

        audit.MapGet("/", async (
            string? entityName,
            string? entityKey,
            int? userId,
            string? actionType,
            DateTime? fromUtc,
            DateTime? toUtc,
            IAuditService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.SearchAsync(
                entityName,
                entityKey,
                userId,
                actionType,
                fromUtc,
                toUtc,
                cancellationToken)));

        app.MapGet("/api/v1/dashboard", async (
            IAuditService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetDashboardAsync(cancellationToken)))
            .WithTags("Dashboard")
            .RequireAuthorization();

        return app;
    }
}
