namespace ArchiveCore.Domain.Entities;

public sealed class AuditEvent
{
    public long AuditEventId { get; set; }
    public int? UserId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? IpAddress { get; set; }
    public Guid? CorrelationId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public User? User { get; set; }
}
