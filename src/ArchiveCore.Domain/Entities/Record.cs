namespace ArchiveCore.Domain.Entities;

public sealed class Record
{
    public long RecordId { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public short RecordStatusId { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    public DateTime OpenedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public RecordStatus Status { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public User? AssignedToUser { get; set; }
    public ICollection<Document> Documents { get; set; } = [];
    public ICollection<RecordMovement> Movements { get; set; } = [];
}
