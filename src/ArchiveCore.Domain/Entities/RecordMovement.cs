namespace ArchiveCore.Domain.Entities;

public sealed class RecordMovement
{
    public long RecordMovementId { get; set; }
    public long RecordId { get; set; }
    public short MovementTypeId { get; set; }
    public int? FromUserId { get; set; }
    public int? ToUserId { get; set; }
    public int PerformedByUserId { get; set; }
    public string? Notes { get; set; }
    public DateTime PerformedAtUtc { get; set; }

    public Record Record { get; set; } = null!;
    public MovementType MovementType { get; set; } = null!;
    public User? FromUser { get; set; }
    public User? ToUser { get; set; }
    public User PerformedByUser { get; set; } = null!;
}
