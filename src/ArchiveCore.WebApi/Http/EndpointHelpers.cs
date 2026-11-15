using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Common;
using ArchiveCore.Infrastructure.Documents;

namespace ArchiveCore.WebApi.Http;

public static class EndpointHelpers
{
    /// <summary>
    /// The authenticated actor id. Endpoints using it always carry an authorization policy,
    /// so a missing id means a malformed token and is reported as 403.
    /// </summary>
    public static int RequireActorId(this ICurrentUser currentUser) =>
        currentUser.UserId ?? throw new ForbiddenOperationException("La sesión no identifica a un usuario válido.");
}

public static class UploadLimits
{
    /// <summary>Multipart overhead tolerated on top of the maximum file size.</summary>
    public const long MultipartOverheadBytes = 1024 * 1024;

    public static long MaxFileSizeBytes(IConfiguration configuration) =>
        configuration.GetValue(
            $"{DocumentStorageOptions.SectionName}:{nameof(DocumentStorageOptions.MaxFileSizeBytes)}",
            new DocumentStorageOptions().MaxFileSizeBytes);

    /// <summary>Kestrel/form limit for the upload request. Keep Nginx client_max_body_size at or above it.</summary>
    public static long RequestLimit(IConfiguration configuration) =>
        MaxFileSizeBytes(configuration) + MultipartOverheadBytes;
}
