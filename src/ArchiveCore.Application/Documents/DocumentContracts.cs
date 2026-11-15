using ArchiveCore.Application.Common;

namespace ArchiveCore.Application.Documents;

public sealed record CreateDocumentRequest(
    long? RecordId,
    int? DocumentCategoryId,
    string? DocumentNumber,
    string? Title,
    string? Description)
{
    public void Validate() =>
        new RequestValidator()
            .Positive(nameof(RecordId), RecordId)
            .Positive(nameof(DocumentCategoryId), DocumentCategoryId)
            .Optional(nameof(DocumentNumber), DocumentNumber, 60)
            .Required(nameof(Title), Title, 200, 3)
            .Optional(nameof(Description), Description, 1000)
            .ThrowIfInvalid();
}

/// <summary>Upload metadata as received from the client. ContentType is informational only.</summary>
public sealed record UploadedFile(
    string FileName,
    string? ClientContentType,
    long Length,
    Stream Content);

public sealed record DocumentSummary(
    long DocumentId,
    long RecordId,
    string Title,
    string? DocumentNumber,
    string Category,
    int CurrentVersion,
    string? CurrentFileName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record DocumentVersionItem(
    long DocumentVersionId,
    int VersionNumber,
    string OriginalFileName,
    string? FileExtension,
    string? MimeType,
    long FileSizeBytes,
    string Sha256Hash,
    int UploadedByUserId,
    string UploadedByName,
    DateTime UploadedAtUtc,
    bool IsCurrent);

public sealed record DocumentDetails(
    long DocumentId,
    long RecordId,
    string RecordNumber,
    string RecordTitle,
    bool RecordIsFinal,
    int DocumentCategoryId,
    string Category,
    string? DocumentNumber,
    string Title,
    string? Description,
    int CurrentVersion,
    string? CurrentFileName,
    int CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<DocumentVersionItem> Versions);

public sealed record DocumentCategoryItem(int DocumentCategoryId, string Code, string Name);

/// <summary>A readable file ready to be streamed to the client.</summary>
public sealed record DocumentDownload(
    Stream Content,
    string ContentType,
    string FileName,
    long Length,
    string Sha256Hash);
