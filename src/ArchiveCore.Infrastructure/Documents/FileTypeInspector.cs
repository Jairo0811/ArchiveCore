using System.Text;

namespace ArchiveCore.Infrastructure.Documents;

public sealed record AllowedFileType(string Extension, string MimeType, FileSignature Signature);

public enum FileSignature
{
    Pdf,
    Png,
    Jpeg,
    Tiff,
    ZipContainer,
    OleCompound,
    PlainText
}

/// <summary>
/// Allow-list of document types. The extension decides the expected type and the leading
/// bytes must match its signature; the client-supplied Content-Type is never trusted and the
/// MIME type stored/served is always the canonical one from this table.
/// </summary>
public static class FileTypeInspector
{
    public const int HeaderLength = 4096;

    private static readonly Dictionary<string, AllowedFileType> Types =
        new AllowedFileType[]
        {
            new(".pdf", "application/pdf", FileSignature.Pdf),
            new(".png", "image/png", FileSignature.Png),
            new(".jpg", "image/jpeg", FileSignature.Jpeg),
            new(".jpeg", "image/jpeg", FileSignature.Jpeg),
            new(".tif", "image/tiff", FileSignature.Tiff),
            new(".tiff", "image/tiff", FileSignature.Tiff),
            new(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", FileSignature.ZipContainer),
            new(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", FileSignature.ZipContainer),
            new(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation", FileSignature.ZipContainer),
            new(".odt", "application/vnd.oasis.opendocument.text", FileSignature.ZipContainer),
            new(".ods", "application/vnd.oasis.opendocument.spreadsheet", FileSignature.ZipContainer),
            new(".doc", "application/msword", FileSignature.OleCompound),
            new(".xls", "application/vnd.ms-excel", FileSignature.OleCompound),
            new(".ppt", "application/vnd.ms-powerpoint", FileSignature.OleCompound),
            new(".txt", "text/plain", FileSignature.PlainText),
            new(".csv", "text/csv", FileSignature.PlainText)
        }.ToDictionary(x => x.Extension, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyCollection<string> AllowedExtensions => Types.Keys;

    public static AllowedFileType? FindByExtension(string? extension) =>
        extension is not null && Types.TryGetValue(extension, out var type) ? type : null;

    /// <summary>Canonical content type for serving a stored version (falls back to octet-stream).</summary>
    public static string ContentTypeFor(string? extension) =>
        FindByExtension(extension)?.MimeType ?? "application/octet-stream";

    public static bool MatchesSignature(AllowedFileType type, ReadOnlySpan<byte> header)
    {
        return type.Signature switch
        {
            FileSignature.Pdf => header.StartsWith("%PDF-"u8),
            FileSignature.Png => header.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            FileSignature.Jpeg => header.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }),
            FileSignature.Tiff => header.StartsWith(new byte[] { 0x49, 0x49, 0x2A, 0x00 })
                                  || header.StartsWith(new byte[] { 0x4D, 0x4D, 0x00, 0x2A }),
            FileSignature.ZipContainer => header.StartsWith(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            FileSignature.OleCompound => header.StartsWith(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            FileSignature.PlainText => LooksLikeText(header),
            _ => false
        };
    }

    private static bool LooksLikeText(ReadOnlySpan<byte> header)
    {
        if (header.Length == 0)
            return false;

        if (header.IndexOf((byte)0) >= 0)
            return false;

        // Reject anything that is not decodable as UTF-8 (a truncated trailing sequence is tolerated).
        var decoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetDecoder();
        try
        {
            var chars = new char[header.Length];
            decoder.Convert(header, chars, flush: false, out _, out _, out _);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>
    /// Produces a safe display/download file name: no directory components, no control or
    /// reserved characters, bounded length, and the original extension preserved.
    /// </summary>
    public static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
        var invalid = Path.GetInvalidFileNameChars().Concat(['"', '<', '>', '|', ':', '*', '?', '/', '\\']).ToHashSet();

        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (char.IsControl(c) || invalid.Contains(c))
                builder.Append('_');
            else
                builder.Append(c);
        }

        var cleaned = builder.ToString().Trim().Trim('.');
        if (cleaned.Length == 0)
            cleaned = "documento";

        if (cleaned.Length > 200)
        {
            var extension = Path.GetExtension(cleaned);
            if (extension.Length > 20)
                extension = string.Empty;
            cleaned = cleaned[..(200 - extension.Length)] + extension;
        }

        return cleaned;
    }
}
