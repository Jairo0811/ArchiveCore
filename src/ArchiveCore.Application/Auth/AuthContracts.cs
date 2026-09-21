namespace ArchiveCore.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? NationalId,
    string? Phone,
    DateOnly? BirthDate,
    IReadOnlyCollection<string> Roles);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserSummary User);

public sealed record UserSummary(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles);
