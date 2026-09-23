namespace ArchiveCore.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse?> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task RevokeAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);
    Task<UserSummary> CreateUserAsync(CreateUserRequest request, int performedByUserId, CancellationToken cancellationToken);
}
