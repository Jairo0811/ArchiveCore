using ArchiveCore.Application.Auditing;
using ArchiveCore.Application.Common;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Auditing;

public sealed class AuditService(ArchiveCoreDbContext db) : IAuditService
{
    public async Task<PagedResult<AuditEventSummary>> SearchAsync(
        AuditQuery request,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var page = PageRequest.From(request.Page, request.PageSize, defaultSize: 50, maxSize: 200);
        var query = db.AuditEvents.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            var entityName = request.EntityName.Trim();
            query = query.Where(x => x.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityKey))
        {
            var entityKey = request.EntityKey.Trim();
            query = query.Where(x => x.EntityKey == entityKey);
        }

        if (request.UserId.HasValue)
            query = query.Where(x => x.UserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.ActionType))
        {
            var actionType = request.ActionType.Trim().ToUpperInvariant();
            query = query.Where(x => x.ActionType == actionType);
        }

        if (request.FromUtc is { } fromUtc)
        {
            var from = AsUtc(fromUtc);
            query = query.Where(x => x.CreatedAtUtc >= from);
        }

        if (request.ToUtc is { } toUtc)
        {
            var to = AsUtc(toUtc);
            query = query.Where(x => x.CreatedAtUtc < to);
        }

        var total = await query.CountAsync(cancellationToken);

        // Old/new JSON snapshots are intentionally not projected: they can contain personal data.
        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.AuditEventId)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(x => new AuditEventSummary(
                x.AuditEventId,
                x.UserId,
                x.User == null ? null : x.User.FirstName + " " + x.User.LastName,
                x.EntityName,
                x.EntityKey,
                x.ActionType,
                x.CreatedAtUtc,
                x.IpAddress,
                x.CorrelationId))
            .ToArrayAsync(cancellationToken);

        return new PagedResult<AuditEventSummary>(items, page.Page, page.PageSize, total);
    }

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };

    public async Task<DashboardSummary> GetDashboardAsync(
        CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var activeUsers = await db.Users.CountAsync(x => x.IsActive, cancellationToken);
        var activeRecords = await db.Records.CountAsync(x => !x.Status.IsFinal, cancellationToken);
        var closedRecords = await db.Records.CountAsync(x => x.Status.IsFinal, cancellationToken);
        var activeDocuments = await db.Documents.CountAsync(x => !x.IsDeleted, cancellationToken);
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
            auditToday,
            DateTime.UtcNow);
    }
}
