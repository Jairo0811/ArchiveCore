using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Records;

namespace ArchiveCore.WebApi.Endpoints;

public static class RecordEndpoints
{
    public static IEndpointRouteBuilder MapRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/records")
            .WithTags("Records")
            .RequireAuthorization();

        group.MapGet("/", async (
            string? search,
            short? statusId,
            int? assignedToUserId,
            IRecordService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(search, statusId, assignedToUserId, cancellationToken)));

        group.MapGet("/{recordId:long}", async (
            long recordId,
            IRecordService service,
            CancellationToken cancellationToken) =>
        {
            var record = await service.GetAsync(recordId, cancellationToken);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        group.MapPost("/", async (
            CreateRecordRequest request,
            IRecordService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            try
            {
                var record = await service.CreateAsync(request, actorId, cancellationToken);
                return Results.Created($"/api/v1/records/{record.RecordId}", record);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPut("/{recordId:long}", async (
            long recordId,
            UpdateRecordRequest request,
            IRecordService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            var record = await service.UpdateAsync(recordId, request, actorId, cancellationToken);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });

        return app;
    }
}
