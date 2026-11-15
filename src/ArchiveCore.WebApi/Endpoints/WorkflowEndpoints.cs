using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Workflow;
using ArchiveCore.WebApi.Http;
using ArchiveCore.WebApi.Security;

namespace ArchiveCore.WebApi.Endpoints;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/records/{recordId:long}/workflow")
            .WithTags("Workflow")
            .RequireAuthorization(AuthorizationPolicies.Readers);

        group.MapGet("/history", async (
            long recordId,
            IWorkflowService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetHistoryAsync(recordId, cancellationToken)));

        // Assigns an unassigned record or transfers it to another active user.
        group.MapPost("/transfer", async (
            long recordId,
            TransferRecordRequest request,
            IWorkflowService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            await service.TransferAsync(recordId, request, currentUser.RequireActorId(), cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.RecordsManagers);

        group.MapPost("/close", async (
            long recordId,
            CloseRecordRequest request,
            IWorkflowService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            await service.CloseAsync(recordId, request, currentUser.RequireActorId(), cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.RecordsManagers);

        return app;
    }
}
