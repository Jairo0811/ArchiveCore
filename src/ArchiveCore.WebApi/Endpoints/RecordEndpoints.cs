using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Records;
using ArchiveCore.WebApi.Http;
using ArchiveCore.WebApi.Security;

namespace ArchiveCore.WebApi.Endpoints;

public static class RecordEndpoints
{
    public static IEndpointRouteBuilder MapRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/records")
            .WithTags("Records")
            .RequireAuthorization(AuthorizationPolicies.Readers);

        group.MapGet("/", async (
            string? search,
            short? statusId,
            int? assignedToUserId,
            bool? unassigned,
            int? page,
            int? pageSize,
            IRecordService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(
                new RecordQuery(search, statusId, assignedToUserId, unassigned, page, pageSize),
                cancellationToken)));

        group.MapGet("/{recordId:long}", async (
            long recordId,
            IRecordService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(recordId, cancellationToken)));

        group.MapPost("/", async (
            CreateRecordRequest request,
            IRecordService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var record = await service.CreateAsync(request, currentUser.RequireActorId(), cancellationToken);
            return Results.Created($"/api/v1/records/{record.RecordId}", record);
        })
        .RequireAuthorization(AuthorizationPolicies.RecordsManagers);

        group.MapPut("/{recordId:long}", async (
            long recordId,
            UpdateRecordRequest request,
            IRecordService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateAsync(recordId, request, currentUser.RequireActorId(), cancellationToken)))
        .RequireAuthorization(AuthorizationPolicies.RecordsManagers);

        return app;
    }
}
