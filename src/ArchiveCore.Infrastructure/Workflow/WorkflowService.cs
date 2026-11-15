using System.Data;
using ArchiveCore.Application.Common;
using ArchiveCore.Application.Workflow;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Workflow;

/// <summary>
/// Workflow mutations are executed by the transactional stored procedures
/// (dbo.sp_TransferRecord, dbo.sp_CloseRecord); EF Core is only used to read history.
/// </summary>
public sealed class WorkflowService(ArchiveCoreDbContext db) : IWorkflowService
{
    public async Task<IReadOnlyCollection<MovementSummary>> GetHistoryAsync(
        long recordId,
        CancellationToken cancellationToken)
    {
        if (!await db.Records.AnyAsync(x => x.RecordId == recordId, cancellationToken))
            throw new ResourceNotFoundException("El expediente no existe.");

        return await db.RecordMovements.AsNoTracking()
            .Where(x => x.RecordId == recordId)
            .OrderByDescending(x => x.PerformedAtUtc)
            .ThenByDescending(x => x.RecordMovementId)
            .Select(x => new MovementSummary(
                x.RecordMovementId,
                x.RecordId,
                x.MovementType.Code,
                x.MovementType.Name,
                x.FromUserId,
                x.FromUser == null ? null : x.FromUser.FirstName + " " + x.FromUser.LastName,
                x.ToUserId,
                x.ToUser == null ? null : x.ToUser.FirstName + " " + x.ToUser.LastName,
                x.PerformedByUserId,
                x.PerformedByUser.FirstName + " " + x.PerformedByUser.LastName,
                x.Notes,
                x.PerformedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task TransferAsync(
        long recordId,
        TransferRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        request.Validate();

        await StoredProcedures.ExecuteScalarAsync<long>(
            db,
            "dbo.sp_TransferRecord",
            [
                StoredProcedures.Param("@RecordId", SqlDbType.BigInt, recordId),
                StoredProcedures.Param("@ToUserId", SqlDbType.Int, request.ToUserId),
                StoredProcedures.Param("@PerformedByUserId", SqlDbType.Int, actorUserId),
                StoredProcedures.Param("@Notes", SqlDbType.NVarChar, NullIfEmpty(request.Notes), 1000)
            ],
            cancellationToken);
    }

    public async Task CloseAsync(
        long recordId,
        CloseRecordRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        request.Validate();

        await StoredProcedures.ExecuteScalarAsync<long>(
            db,
            "dbo.sp_CloseRecord",
            [
                StoredProcedures.Param("@RecordId", SqlDbType.BigInt, recordId),
                StoredProcedures.Param("@PerformedByUserId", SqlDbType.Int, actorUserId),
                StoredProcedures.Param("@Notes", SqlDbType.NVarChar, NullIfEmpty(request.Notes), 1000)
            ],
            cancellationToken);
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
