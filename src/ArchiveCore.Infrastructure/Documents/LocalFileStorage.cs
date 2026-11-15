using System.Security.Cryptography;
using ArchiveCore.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveCore.Infrastructure.Documents;

public sealed record StoredFile(
    string StoredFileName,
    string RelativePath,
    string Sha256Hash,
    long SizeBytes);

/// <summary>
/// Local file-system storage. Only paths relative to the configured root are persisted in
/// SQL Server, so physical locations are never exposed through the API, audit or UI.
/// </summary>
public sealed class LocalFileStorage
{
    private readonly string _rootPath;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(
        IOptions<DocumentStorageOptions> options,
        IWebHostEnvironment environment,
        ILogger<LocalFileStorage> logger)
    {
        _logger = logger;
        var configured = options.Value.RootPath;
        var root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured);

        _rootPath = Path.GetFullPath(root);
    }

    public string RootPath => _rootPath;

    /// <summary>
    /// Streams the content to a new, uniquely named file while computing its SHA-256 hash.
    /// Writes to a temporary name first and renames on success, so partial files never
    /// look like valid documents.
    /// </summary>
    public async Task<StoredFile> SaveAsync(
        long documentId,
        string extension,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var relativePath = $"{documentId}/{storedName}";
        var finalPath = ResolveSafe(relativePath);
        var tempPath = finalPath + ".uploading";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

            long total = 0;
            using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            await using (var output = new FileStream(
                tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    total += read;
                    if (total > maxBytes)
                        throw new RequestValidationException("File", "El archivo supera el tamaño máximo permitido.");

                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    sha.AppendData(buffer, 0, read);
                }

                await output.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, finalPath);

            return new StoredFile(
                storedName,
                relativePath,
                Convert.ToHexString(sha.GetHashAndReset()),
                total);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(tempPath);
            TryDelete(finalPath);
            _logger.LogError(ex, "Document storage write failed for document {DocumentId}", documentId);
            throw new StorageFailureException("No se pudo almacenar el archivo en el servidor.", ex);
        }
        catch
        {
            TryDelete(tempPath);
            TryDelete(finalPath);
            throw;
        }
    }

    public Stream OpenRead(string storedPath)
    {
        var path = ResolveStored(storedPath);

        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            _logger.LogError("Stored document file is missing (relative path {RelativePath})", storedPath);
            throw new ResourceNotFoundException("El archivo de esta versión no está disponible.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Stored document file could not be opened");
            throw new StorageFailureException("No se pudo leer el archivo almacenado.", ex);
        }
    }

    /// <summary>Best-effort removal used to clean up orphans when the database operation fails.</summary>
    public void TryDeleteStored(string relativePath)
    {
        try
        {
            TryDelete(ResolveSafe(relativePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not remove orphan document file");
        }
    }

    /// <summary>
    /// Resolves a stored path. Rows created by older builds stored an absolute path; those are
    /// accepted only when they live inside the configured root.
    /// </summary>
    private string ResolveStored(string storedPath) =>
        Path.IsPathRooted(storedPath) ? EnsureInsideRoot(Path.GetFullPath(storedPath)) : ResolveSafe(storedPath);

    private string ResolveSafe(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new StorageFailureException("Ruta de almacenamiento no válida.");

        return EnsureInsideRoot(Path.GetFullPath(Path.Combine(_rootPath, relativePath)));
    }

    private string EnsureInsideRoot(string fullPath)
    {
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            throw new StorageFailureException("Ruta de almacenamiento no válida.");

        return fullPath;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
