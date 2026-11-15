namespace ArchiveCore.Infrastructure.Documents;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    /// <summary>
    /// Root folder for physical files. Relative paths are resolved against the content root.
    /// In Docker this is a named volume mounted at /app/storage/documents.
    /// </summary>
    public string RootPath { get; set; } = "storage/documents";

    /// <summary>Maximum accepted file size in bytes (default 20 MB). Keep Nginx client_max_body_size above it.</summary>
    public long MaxFileSizeBytes { get; set; } = 20 * 1024 * 1024;
}
