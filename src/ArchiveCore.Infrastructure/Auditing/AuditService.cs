using ArchiveCore.Application.Auditing;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Auditing;

public sealed class AuditService(ArchiveCoreDbContext db) : IAuditService
{
    public async Task<IReadOnlyCollection<AuditEventSummary>> SearchAsync(
        string? entityName,
        string? entityKey,
        int? userId,
        string? actionType,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var query = db.AuditEvents.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(x => x.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(entityKey))
            query = query.Where(x => x.EntityKey == entityKey);

        if (userId.HasValue)
            query = query.Where(x => x.UserId == userId);

        if (!string.IsNullOrWhiteSpace(actionType))
            query = query.Where(x => x.ActionType == actionType);

        if (fromUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(x => x.CreatedAtUtc < toUtc.Value);

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.AuditEventId)
            .Take(500)
            .Select(x => new AuditEventSummary(
                x.AuditEventId,
                x.UserId,
                x.EntityName,
                x.EntityKey,
                x.ActionType,
                x.CreatedAtUtc,
                x.IpAddress,
                x.CorrelationId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<DashboardSummary> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var activeUsers = await db.Users.CountAsync(x => x.IsActive, cancellationToken);
        var activeRecords = await db.Records.CountAsync(
            x => x.ClosedAtUtc == null,
            cancellationToken);
        var closedRecords = await db.Records.CountAsync(
            x => x.ClosedAtUtc != null,
            cancellationToken);
        var activeDocuments = await db.Documents.CountAsync(
            x => !x.IsDeleted,
            cancellationToken);
        var movementsToday = await db.RecordMovements.CountAsync(
            x => x.PerformedAtUtc >= today && x.PerformedAtUtc < tomorrow,
            cancellationToken);
        var auditToday = await db.AuditEvents.CountAsync(
            x => x.CreatedAtUtc >= today && x.CreatedAtUtc < tomorrow,
            cancellationToken);

        return new DashboardSummary(
            activeUsers,
            activeRecords,
            closedRecords,
            activeDocuments,
            movementsToday,
            auditToday);
    }
}
