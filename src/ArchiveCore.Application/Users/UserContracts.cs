using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Common;

namespace ArchiveCore.Application.Users;

public sealed record CreateUserRequest(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Password,
    string? NationalId,
    string? Phone,
    DateOnly? BirthDate,
    IReadOnlyCollection<string>? Roles,
    bool? IsActive)
{
    public void Validate()
    {
        var v = new RequestValidator()
            .Required(nameof(FirstName), FirstName, 100)
            .Required(nameof(LastName), LastName, 100)
            .Email(nameof(Email), Email)
            .Optional(nameof(NationalId), NationalId, 20)
            .Optional(nameof(Phone), Phone, 25);

        foreach (var error in PasswordPolicy.Validate(Password))
            v.Add(nameof(Password), error);

        if (BirthDate is { } birthDate &&
            (birthDate > DateOnly.FromDateTime(DateTime.UtcNow) || birthDate.Year < 1900))
            v.Add(nameof(BirthDate), "La fecha de nacimiento no es válida.");

        var roles = Roles?.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() ?? [];
        if (roles.Length == 0)
            v.Add(nameof(Roles), "Debe asignar al menos un rol.");
        else if (roles.Any(r => !RoleNames.All.Contains(r.Trim(), StringComparer.Ordinal)))
            v.Add(nameof(Roles), "Uno o más roles no son válidos.");

        v.ThrowIfInvalid();
    }
}

public sealed record UpdateUserStatusRequest(bool? IsActive)
{
    public void Validate()
    {
        if (IsActive is null)
            throw new RequestValidationException(nameof(IsActive), "Este campo es obligatorio.");
    }
}

public sealed record UserListItem(
    int UserId,
    string FirstName,
    string LastName,
    string Email,
    bool IsActive,
    IReadOnlyCollection<string> Roles,
    DateTime CreatedAtUtc);

/// <summary>Minimal, non-sensitive user projection for assignment pickers.</summary>
public sealed record UserLookupItem(int UserId, string FullName);

public sealed record RoleItem(short RoleId, string Name, string? Description);

public interface IUserAdminService
{
    Task<IReadOnlyCollection<UserListItem>> ListAsync(CancellationToken cancellationToken);

    Task<UserListItem> CreateAsync(CreateUserRequest request, int actorUserId, CancellationToken cancellationToken);

    Task<UserListItem> SetStatusAsync(int userId, bool isActive, int actorUserId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserLookupItem>> LookupActiveAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RoleItem>> ListRolesAsync(CancellationToken cancellationToken);
}
