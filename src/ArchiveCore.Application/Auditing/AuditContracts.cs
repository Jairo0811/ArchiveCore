using ArchiveCore.Application.Common;

namespace ArchiveCore.Application.Auditing;

public sealed record AuditQuery(
    string? EntityName,
    string? EntityKey,
    int? UserId,
    string? ActionType,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int? Page,
    int? PageSize)
{
    public static readonly IReadOnlyList<string> ActionTypes =
        ["INSERT", "UPDATE", "DELETE", "LOGIN", "LOGOUT", "DOWNLOAD", "OTHER"];

    public static readonly IReadOnlyList<string> EntityNames =
        ["Records", "Documents", "DocumentVersions", "Users"];

    public void Validate()
    {
        var v = new RequestValidator()
            .Optional(nameof(EntityName), EntityName, 128)
            .Optional(nameof(EntityKey), EntityKey, 200)
            .Positive(nameof(UserId), UserId, required: false);

        if (!string.IsNullOrWhiteSpace(ActionType) &&
            !ActionTypes.Contains(ActionType.Trim().ToUpperInvariant(), StringComparer.Ordinal))
            v.Add(nameof(ActionType), "La acción no es válida.");

        if (FromUtc is { } from && ToUtc is { } to && from > to)
            v.Add(nameof(FromUtc), "La fecha inicial no puede ser posterior a la final.");

        v.ThrowIfInvalid();
    }
}

public sealed record AuditEventSummary(
    long AuditEventId,
    int? UserId,
    string? UserName,
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
    int AuditEventsToday,
    DateTime GeneratedAtUtc);

public interface IAuditService
{
    Task<PagedResult<AuditEventSummary>> SearchAsync(AuditQuery query, CancellationToken cancellationToken);

    Task<DashboardSummary> GetDashboardAsync(CancellationToken cancellationToken);
}
