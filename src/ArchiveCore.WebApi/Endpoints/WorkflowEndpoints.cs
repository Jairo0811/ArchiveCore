using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Workflow;

namespace ArchiveCore.WebApi.Endpoints;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/records/{recordId:long}/workflow")
            .WithTags("Workflow")
            .RequireAuthorization();

        group.MapGet("/history", async (
            long recordId,
            IWorkflowService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetHistoryAsync(recordId, cancellationToken)));

        group.MapPost("/transfer", async (
            long recordId,
            TransferRecordRequest request,
            IWorkflowService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            try
            {
                await service.TransferAsync(recordId, request, actorId, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/close", async (
            long recordId,
            CloseRecordRequest request,
            IWorkflowService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            try
            {
                await service.CloseAsync(recordId, request, actorId, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }
}
