namespace ArchiveCore.Application.Documents;

public interface IDocumentService
{
    Task<IReadOnlyCollection<DocumentSummary>> ListByRecordAsync(
        long recordId,
        CancellationToken cancellationToken);

    Task<DocumentDetails?> GetAsync(long documentId, CancellationToken cancellationToken);

    Task<DocumentDetails> CreateAsync(
        CreateDocumentRequest request,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<DocumentDetails> AddVersionAsync(
        long documentId,
        AddDocumentVersionRequest request,
        Stream content,
        int actorUserId,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(
        long documentId,
        int actorUserId,
        CancellationToken cancellationToken);
}
