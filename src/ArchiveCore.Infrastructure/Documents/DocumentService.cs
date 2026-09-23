using ArchiveCore.Application.Documents;
using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Documents;

public sealed class DocumentService(
    ArchiveCoreDbContext db,
    LocalFileStorage storage) : IDocumentService
{
    public async Task<IReadOnlyCollection<DocumentSummary>> ListByRecordAsync(
        long recordId,
        CancellationToken cancellationToken) =>
        await db.Documents.AsNoTracking()
            .Where(x => x.RecordId == recordId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DocumentSummary(
                x.DocumentId,
                x.RecordId,
                x.Title,
                x.DocumentNumber,
                x.Category.Name,
                x.Versions.Where(v => v.IsCurrent)
                    .Select(v => v.VersionNumber)
                    .FirstOrDefault(),
                x.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

    public async Task<DocumentDetails?> GetAsync(
        long documentId,
        CancellationToken cancellationToken) =>
        await db.Documents.AsNoTracking()
            .Where(x => x.DocumentId == documentId && !x.IsDeleted)
            .Select(x => new DocumentDetails(
                x.DocumentId,
                x.RecordId,
                x.DocumentCategoryId,
                x.Category.Name,
                x.DocumentNumber,
                x.Title,
                x.Description,
                x.Versions.Where(v => v.IsCurrent)
                    .Select(v => v.VersionNumber)
                    .FirstOrDefault(),
                x.Versions.Where(v => v.IsCurrent)
                    .Select(v => v.OriginalFileName)
                    .FirstOrDefault(),
                x.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<DocumentDetails> CreateAsync(
        CreateDocumentRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (!await db.Records.AnyAsync(x => x.RecordId == request.RecordId, cancellationToken))
            throw new InvalidOperationException("Record does not exist.");

        if (!await db.DocumentCategories.AnyAsync(
            x => x.DocumentCategoryId == request.DocumentCategoryId && x.IsActive,
            cancellationToken))
            throw new InvalidOperationException("Document category does not exist or is inactive.");

        var entity = new Document
        {
            RecordId = request.RecordId,
            DocumentCategoryId = request.DocumentCategoryId,
            DocumentNumber = request.DocumentNumber?.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            CreatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Documents.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(entity.DocumentId, cancellationToken))!;
    }

    public async Task<DocumentDetails> AddVersionAsync(
        long documentId,
        AddDocumentVersionRequest request,
        Stream content,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .Include(x => x.Versions)
            .SingleOrDefaultAsync(
                x => x.DocumentId == documentId && !x.IsDeleted,
                cancellationToken)
            ?? throw new InvalidOperationException("Document does not exist.");

        var stored = await storage.SaveAsync(
            documentId,
            request.OriginalFileName,
            content,
            cancellationToken);

        foreach (var version in document.Versions.Where(x => x.IsCurrent))
            version.IsCurrent = false;

        var nextVersion = document.Versions.Count == 0
            ? 1
            : document.Versions.Max(x => x.VersionNumber) + 1;

        document.Versions.Add(new DocumentVersion
        {
            VersionNumber = nextVersion,
            OriginalFileName = request.OriginalFileName,
            StoredFileName = stored.StoredFileName,
            StoragePath = stored.StoragePath,
            FileExtension = Path.GetExtension(request.OriginalFileName),
            MimeType = request.ContentType,
            FileSizeBytes = request.FileSizeBytes,
            Sha256Hash = stored.Sha256Hash,
            UploadedByUserId = actorUserId,
            UploadedAtUtc = DateTime.UtcNow,
            IsCurrent = true
        });

        document.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(documentId, cancellationToken))!;
    }

    public async Task<bool> DeleteAsync(
        long documentId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents.SingleOrDefaultAsync(
            x => x.DocumentId == documentId && !x.IsDeleted,
            cancellationToken);

        if (document is null)
            return false;

        document.IsDeleted = true;
        document.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
