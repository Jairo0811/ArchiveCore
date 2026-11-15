using ArchiveCore.Application.Common;

namespace ArchiveCore.Application.Auth;

public sealed record LoginRequest(string? Email, string? Password)
{
    public void Validate()
    {
        var v = new RequestValidator();

        if (string.IsNullOrWhiteSpace(Email))
            v.Add(nameof(Email), "El correo es obligatorio.");
        else if (Email.Trim().Length > 256)
            v.Add(nameof(Email), "No puede superar 256 caracteres.");

        if (string.IsNullOrEmpty(Password))
            v.Add(nameof(Password), "La contraseña es obligatoria.");
        else if (Password.Length > PasswordPolicy.MaxLength)
            v.Add(nameof(Password), $"No puede superar {PasswordPolicy.MaxLength} caracteres.");

        v.ThrowIfInvalid();
    }
}

/// <summary>
/// Result of a successful login/refresh. The raw refresh token is only handed to the
/// HTTP layer, which stores it in an HttpOnly cookie; it is never serialized to the body.
/// </summary>
public sealed record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserSummary User);

/// <summary>Body returned to the SPA after login/refresh.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    UserSummary User);

public sealed record UserSummary(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyCollection<string> Roles);
