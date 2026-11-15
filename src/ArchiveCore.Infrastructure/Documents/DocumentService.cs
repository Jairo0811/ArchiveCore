using System.Data;
using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Common;
using ArchiveCore.Application.Documents;
using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArchiveCore.Infrastructure.Documents;

public sealed class DocumentService(
    ArchiveCoreDbContext db,
    LocalFileStorage storage,
    IOptions<DocumentStorageOptions> storageOptions,
    ICurrentUser currentUser) : IDocumentService
{
    private readonly DocumentStorageOptions _options = storageOptions.Value;

    public async Task<IReadOnlyCollection<DocumentSummary>> ListByRecordAsync(
        long recordId,
        CancellationToken cancellationToken)
    {
        if (!await db.Records.AnyAsync(x => x.RecordId == recordId, cancellationToken))
            throw new ResourceNotFoundException("El expediente no existe.");

        return await db.Documents.AsNoTracking()
            .Where(x => x.RecordId == recordId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.DocumentId)
            .Take(500)
            .Select(x => new DocumentSummary(
                x.DocumentId,
                x.RecordId,
                x.Title,
                x.DocumentNumber,
                x.Category.Name,
                x.Versions.Where(v => v.IsCurrent).Select(v => v.VersionNumber).FirstOrDefault(),
                x.Versions.Where(v => v.IsCurrent).Select(v => v.OriginalFileName).FirstOrDefault(),
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<DocumentDetails> GetAsync(long documentId, CancellationToken cancellationToken)
    {
        var document = await db.Documents.AsNoTracking()
            .Where(x => x.DocumentId == documentId && !x.IsDeleted)
            .Select(x => new
            {
                x.DocumentId,
                x.RecordId,
                x.Record.RecordNumber,
                RecordTitle = x.Record.Title,
                RecordIsFinal = x.Record.Status.IsFinal,
                x.DocumentCategoryId,
                Category = x.Category.Name,
                x.DocumentNumber,
                x.Title,
                x.Description,
                x.CreatedByUserId,
                CreatedByName = x.CreatedByUser.FirstName + " " + x.CreatedByUser.LastName,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("El documento no existe o fue eliminado.");

        var versions = await db.DocumentVersions.AsNoTracking()
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new DocumentVersionItem(
                v.DocumentVersionId,
                v.VersionNumber,
                v.OriginalFileName,
                v.FileExtension,
                v.MimeType,
                v.FileSizeBytes,
                v.Sha256Hash,
                v.UploadedByUserId,
                v.UploadedByUser.FirstName + " " + v.UploadedByUser.LastName,
                v.UploadedAtUtc,
                v.IsCurrent))
            .ToArrayAsync(cancellationToken);

        var current = versions.FirstOrDefault(v => v.IsCurrent);

        return new DocumentDetails(
            document.DocumentId,
            document.RecordId,
            document.RecordNumber,
            document.RecordTitle,
            document.RecordIsFinal,
            document.DocumentCategoryId,
            document.Category,
            document.DocumentNumber,
            document.Title,
            document.Description,
            current?.VersionNumber ?? 0,
            current?.OriginalFileName,
            document.CreatedByUserId,
            document.CreatedByName,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            versions);
    }

    public async Task<DocumentDetails> CreateAsync(
        CreateDocumentRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var record = await db.Records.AsNoTracking()
            .Where(x => x.RecordId == request.RecordId)
            .Select(x => new { x.RecordId, x.Status.IsFinal })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RequestValidationException(nameof(request.RecordId), "El expediente no existe.");

        if (record.IsFinal)
            throw new ConflictException("El expediente está cerrado y no admite nuevos documentos.");

        if (!await db.DocumentCategories.AnyAsync(
                x => x.DocumentCategoryId == request.DocumentCategoryId && x.IsActive,
                cancellationToken))
            throw new RequestValidationException(nameof(request.DocumentCategoryId), "La categoría no existe o está inactiva.");

        var entity = new Document
        {
            RecordId = record.RecordId,
            DocumentCategoryId = request.DocumentCategoryId!.Value,
            DocumentNumber = NullIfEmpty(request.DocumentNumber),
            Title = request.Title!.Trim(),
            Description = NullIfEmpty(request.Description),
            CreatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.Documents.Add(entity);
        await SqlErrorTranslator.RunAsync(() => db.SaveChangesAsync(cancellationToken));

        return await GetAsync(entity.DocumentId, cancellationToken);
    }

    public async Task<DocumentDetails> AddVersionAsync(
        long documentId,
        UploadedFile file,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var state = await db.Documents.AsNoTracking()
            .Where(x => x.DocumentId == documentId && !x.IsDeleted)
            .Select(x => new { x.Record.Status.IsFinal })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("El documento no existe o fue eliminado.");

        if (state.IsFinal)
            throw new ConflictException("El expediente del documento está cerrado; no admite nuevas versiones.");

        var originalName = FileTypeInspector.SanitizeFileName(file.FileName);
        var type = ValidateFile(file, originalName);

        var stored = await storage.SaveAsync(
            documentId,
            type.Extension,
            file.Content,
            _options.MaxFileSizeBytes,
            cancellationToken);

        try
        {
            await StoredProcedures.ExecuteScalarAsync<long>(
                db,
                "dbo.sp_AddDocumentVersion",
                [
                    StoredProcedures.Param("@DocumentId", SqlDbType.BigInt, documentId),
                    StoredProcedures.Param("@OriginalFileName", SqlDbType.NVarChar, originalName, 260),
                    StoredProcedures.Param("@StoredFileName", SqlDbType.NVarChar, stored.StoredFileName, 260),
                    StoredProcedures.Param("@StoragePath", SqlDbType.NVarChar, stored.RelativePath, 1000),
                    StoredProcedures.Param("@FileExtension", SqlDbType.VarChar, type.Extension, 20),
                    StoredProcedures.Param("@MimeType", SqlDbType.NVarChar, type.MimeType, 150),
                    StoredProcedures.Param("@FileSizeBytes", SqlDbType.BigInt, stored.SizeBytes),
                    StoredProcedures.Param("@Sha256Hash", SqlDbType.Char, stored.Sha256Hash, 64),
                    StoredProcedures.Param("@UploadedByUserId", SqlDbType.Int, actorUserId)
                ],
                // The file is already on disk: do not abandon the DB step half-way if the client disconnects.
                CancellationToken.None);
        }
        catch
        {
            // Keep storage and database consistent: no orphan files for failed registrations.
            storage.TryDeleteStored(stored.RelativePath);
            throw;
        }

        return await GetAsync(documentId, cancellationToken);
    }

    public async Task<DocumentDownload> OpenDownloadAsync(
        long documentId,
        int? versionNumber,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var query = db.DocumentVersions.AsNoTracking()
            .Where(v => v.DocumentId == documentId && !v.Document.IsDeleted);

        query = versionNumber is null
            ? query.Where(v => v.IsCurrent)
            : query.Where(v => v.VersionNumber == versionNumber.Value);

        var version = await query
            .Select(v => new
            {
                v.DocumentVersionId,
                v.OriginalFileName,
                v.FileExtension,
                v.StoragePath,
                v.FileSizeBytes,
                v.Sha256Hash
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new ResourceNotFoundException("La versión solicitada no existe.");

        var stream = storage.OpenRead(version.StoragePath);

        db.AuditEvents.Add(new AuditEvent
        {
            UserId = actorUserId,
            EntityName = "DocumentVersions",
            EntityKey = version.DocumentVersionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ActionType = "DOWNLOAD",
            IpAddress = currentUser.IpAddress is { Length: > 45 } ip ? ip[..45] : currentUser.IpAddress,
            CorrelationId = currentUser.CorrelationId,
            CreatedAtUtc = DateTime.UtcNow
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }

        return new DocumentDownload(
            stream,
            FileTypeInspector.ContentTypeFor(version.FileExtension),
            FileTypeInspector.SanitizeFileName(version.OriginalFileName),
            version.FileSizeBytes,
            version.Sha256Hash.Trim());
    }

    public async Task DeleteAsync(long documentId, int actorUserId, CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .Include(x => x.Record).ThenInclude(x => x.Status)
            .SingleOrDefaultAsync(x => x.DocumentId == documentId && !x.IsDeleted, cancellationToken)
            ?? throw new ResourceNotFoundException("El documento no existe o fue eliminado.");

        if (document.Record.Status.IsFinal)
            throw new ConflictException("El expediente está cerrado; sus documentos no pueden eliminarse.");

        document.IsDeleted = true;
        document.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("El documento fue modificado por otro usuario. Recarga e inténtalo de nuevo.");
        }
    }

    public async Task<IReadOnlyCollection<DocumentCategoryItem>> ListCategoriesAsync(CancellationToken cancellationToken) =>
        await db.DocumentCategories.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new DocumentCategoryItem(x.DocumentCategoryId, x.Code, x.Name))
            .ToArrayAsync(cancellationToken);

    private AllowedFileType ValidateFile(UploadedFile file, string sanitizedName)
    {
        if (file.Length <= 0)
            throw new RequestValidationException("File", "El archivo está vacío.");

        if (file.Length > _options.MaxFileSizeBytes)
            throw new RequestValidationException(
                "File",
                $"El archivo supera el tamaño máximo permitido ({_options.MaxFileSizeBytes / (1024 * 1024)} MB).");

        var type = FileTypeInspector.FindByExtension(Path.GetExtension(sanitizedName))
            ?? throw new RequestValidationException(
                "File",
                "Tipo de archivo no permitido. Formatos aceptados: " +
                string.Join(", ", FileTypeInspector.AllowedExtensions.Order()) + ".");

        if (!file.Content.CanSeek)
            throw new RequestValidationException("File", "No se pudo leer el archivo.");

        var header = new byte[FileTypeInspector.HeaderLength];
        var read = 0;
        while (read < header.Length)
        {
            var n = file.Content.Read(header, read, header.Length - read);
            if (n == 0)
                break;
            read += n;
        }

        file.Content.Seek(0, SeekOrigin.Begin);

        if (!FileTypeInspector.MatchesSignature(type, header.AsSpan(0, read)))
            throw new RequestValidationException(
                "File",
                "El contenido del archivo no corresponde con su extensión.");

        return type;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
