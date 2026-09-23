using System.Security.Cryptography;

namespace ArchiveCore.Infrastructure.Documents;

public sealed record StoredFile(
    string StoredFileName,
    string StoragePath,
    string Sha256Hash);

public sealed class LocalFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage()
    {
        _rootPath = Path.Combine(AppContext.BaseDirectory, "storage", "documents");
    }

    public async Task<StoredFile> SaveAsync(
        long documentId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(originalFileName);
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var directory = Path.Combine(_rootPath, documentId.ToString());
        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, storedName);

        await using var output = File.Create(fullPath);
        using var sha = SHA256.Create();

        var buffer = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            sha.TransformBlock(buffer, 0, read, null, 0);
        }

        sha.TransformFinalBlock([], 0, 0);

        return new StoredFile(
            storedName,
            fullPath,
            Convert.ToHexString(sha.Hash!));
    }
}
