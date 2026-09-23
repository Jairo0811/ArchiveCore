using ArchiveCore.Application.Workflow;
using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Workflow;

public sealed class WorkflowService(ArchiveCoreDbContext db) : IWorkflowService
{
    public async Task<IReadOnlyCollection<MovementSummary>> GetHistoryAsync(
        long recordId,
        CancellationToken cancellationToken) =>
        await db.RecordMovements.AsNoTracking()
            .Where(x => x.RecordId == recordId)
            .OrderByDescending(x => x.PerformedAtUtc)
            .Select(x => new MovementSummary(
                x.RecordMovementId,
                x.RecordId,
                x.MovementType.Name,
                x.FromUserId,
                x.ToUserId,
                x.PerformedByUserId,
                x.Notes,
                x.PerformedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task TransferAsync(
        long recordId,
        TransferRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var record = await db.Records.SingleOrDefaultAsync(
            x => x.RecordId == recordId,
            cancellationToken)
            ?? throw new InvalidOperationException("Record does not exist.");

        var movementType = await db.MovementTypes.SingleAsync(
            x => x.Code == "TRANSFER",
            cancellationToken);

        var fromUserId = record.AssignedToUserId;
        record.AssignedToUserId = request.ToUserId;
        record.UpdatedAtUtc = DateTime.UtcNow;

        db.RecordMovements.Add(new RecordMovement
        {
            RecordId = recordId,
            MovementTypeId = movementType.MovementTypeId,
            FromUserId = fromUserId,
            ToUserId = request.ToUserId,
            PerformedByUserId = actorUserId,
            Notes = request.Notes?.Trim(),
            PerformedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseAsync(
        long recordId,
        CloseRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var record = await db.Records.SingleOrDefaultAsync(
            x => x.RecordId == recordId,
            cancellationToken)
            ?? throw new InvalidOperationException("Record does not exist.");

        var closedStatus = await db.RecordStatuses.SingleAsync(
            x => x.Code == "CLOSED",
            cancellationToken);

        var movementType = await db.MovementTypes.SingleAsync(
            x => x.Code == "CLOSE",
            cancellationToken);

        record.RecordStatusId = closedStatus.RecordStatusId;
        record.ClosedAtUtc = DateTime.UtcNow;
        record.UpdatedAtUtc = DateTime.UtcNow;

        db.RecordMovements.Add(new RecordMovement
        {
            RecordId = recordId,
            MovementTypeId = movementType.MovementTypeId,
            FromUserId = record.AssignedToUserId,
            ToUserId = record.AssignedToUserId,
            PerformedByUserId = actorUserId,
            Notes = request.Notes?.Trim(),
            PerformedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
