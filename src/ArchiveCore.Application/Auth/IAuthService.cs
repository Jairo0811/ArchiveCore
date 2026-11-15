namespace ArchiveCore.Application.Auth;

public interface IAuthService
{
    /// <summary>Returns null when the credentials are invalid or the user is inactive.</summary>
    Task<AuthResult?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Rotates a refresh token. Returns null when the token is unknown, expired or revoked.</summary>
    Task<AuthResult?> RefreshAsync(string? refreshToken, string? ipAddress, CancellationToken cancellationToken);

    Task RevokeAsync(string? refreshToken, string? ipAddress, CancellationToken cancellationToken);
}
