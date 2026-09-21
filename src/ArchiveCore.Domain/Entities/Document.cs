namespace ArchiveCore.Domain.Entities;

public sealed class Document
{
    public long DocumentId { get; set; }
    public long RecordId { get; set; }
    public int DocumentCategoryId { get; set; }
    public string? DocumentNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public int CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public Record Record { get; set; } = null!;
    public DocumentCategory Category { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<DocumentVersion> Versions { get; set; } = [];
}
