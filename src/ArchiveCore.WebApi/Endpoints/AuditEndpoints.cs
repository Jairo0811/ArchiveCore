using ArchiveCore.Application.Auditing;
using ArchiveCore.WebApi.Security;

namespace ArchiveCore.WebApi.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/audit", async (
            string? entityName,
            string? entityKey,
            int? userId,
            string? actionType,
            DateTime? fromUtc,
            DateTime? toUtc,
            int? page,
            int? pageSize,
            IAuditService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.SearchAsync(
                new AuditQuery(entityName, entityKey, userId, actionType, fromUtc, toUtc, page, pageSize),
                cancellationToken)))
        .WithTags("Audit")
        .RequireAuthorization(AuthorizationPolicies.Administrators);

        app.MapGet("/api/v1/dashboard", async (
            IAuditService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetDashboardAsync(cancellationToken)))
        .WithTags("Dashboard")
        .RequireAuthorization(AuthorizationPolicies.Readers);

        return app;
    }
}
