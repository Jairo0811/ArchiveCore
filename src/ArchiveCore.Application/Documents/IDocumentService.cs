namespace ArchiveCore.Application.Documents;

public interface IDocumentService
{
    Task<IReadOnlyCollection<DocumentSummary>> ListByRecordAsync(
        long recordId,
        CancellationToken cancellationToken);

    Task<DocumentDetails> GetAsync(long documentId, CancellationToken cancellationToken);

    Task<DocumentDetails> CreateAsync(
        CreateDocumentRequest request,
        int actorUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validates, stores and registers a new version through dbo.sp_AddDocumentVersion.
    /// The physical file is removed if the database operation fails.
    /// </summary>
    Task<DocumentDetails> AddVersionAsync(
        long documentId,
        UploadedFile file,
        int actorUserId,
        CancellationToken cancellationToken);

    /// <summary>Opens a version (current when versionNumber is null) and records a DOWNLOAD audit event.</summary>
    Task<DocumentDownload> OpenDownloadAsync(
        long documentId,
        int? versionNumber,
        int actorUserId,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        long documentId,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DocumentCategoryItem>> ListCategoriesAsync(CancellationToken cancellationToken);
}
