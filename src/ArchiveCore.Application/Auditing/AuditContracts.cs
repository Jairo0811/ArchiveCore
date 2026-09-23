namespace ArchiveCore.Application.Auditing;

public sealed record AuditEventSummary(
    long AuditEventId,
    int? UserId,
    string EntityName,
    string EntityKey,
    string ActionType,
    DateTime CreatedAtUtc,
    string? IpAddress,
    Guid? CorrelationId);

public sealed record DashboardSummary(
    int ActiveUsers,
    int ActiveRecords,
    int ClosedRecords,
    int ActiveDocuments,
    int MovementsToday,
    int AuditEventsToday);

public interface IAuditService
{
    Task<IReadOnlyCollection<AuditEventSummary>> SearchAsync(
        string? entityName,
        string? entityKey,
        int? userId,
        string? actionType,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);

    Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken);
}
