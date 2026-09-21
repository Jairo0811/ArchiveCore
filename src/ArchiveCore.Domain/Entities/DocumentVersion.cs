namespace ArchiveCore.Domain.Entities;

public sealed class DocumentVersion
{
    public long DocumentVersionId { get; set; }
    public long DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? FileExtension { get; set; }
    public string? MimeType { get; set; }
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public bool IsCurrent { get; set; } = true;

    public Document Document { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}
