namespace ArchiveCore.Application.Documents;

public sealed record CreateDocumentRequest(
    long RecordId,
    int DocumentCategoryId,
    string? DocumentNumber,
    string Title,
    string? Description);

public sealed record AddDocumentVersionRequest(
    string OriginalFileName,
    string? ContentType,
    long FileSizeBytes);

public sealed record DocumentSummary(
    long DocumentId,
    long RecordId,
    string Title,
    string? DocumentNumber,
    string Category,
    int CurrentVersion,
    DateTime CreatedAtUtc);

public sealed record DocumentDetails(
    long DocumentId,
    long RecordId,
    int DocumentCategoryId,
    string Category,
    string? DocumentNumber,
    string Title,
    string? Description,
    int CurrentVersion,
    string? CurrentFileName,
    DateTime CreatedAtUtc);
