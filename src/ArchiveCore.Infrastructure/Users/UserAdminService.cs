using ArchiveCore.Application.Common;
using ArchiveCore.Application.Users;
using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using ArchiveCore.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.Infrastructure.Users;

/// <summary>Minimal administrator-only user provisioning (no full IAM by design).</summary>
public sealed class UserAdminService(
    ArchiveCoreDbContext db,
    PasswordService passwordService) : IUserAdminService
{
    public async Task<IReadOnlyCollection<UserListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new
            {
                x.UserId,
                x.FirstName,
                x.LastName,
                x.Email,
                x.IsActive,
                x.CreatedAtUtc,
                Roles = x.UserRoles.Select(r => r.Role.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        return users
            .Select(x => new UserListItem(
                x.UserId, x.FirstName, x.LastName, x.Email, x.IsActive,
                x.Roles.Order(StringComparer.Ordinal).ToArray(), x.CreatedAtUtc))
            .ToArray();
    }

    public async Task<UserListItem> CreateAsync(
        CreateUserRequest request,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var email = request.Email!.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            throw new ConflictException("Ya existe un usuario con ese correo.");

        var nationalId = NullIfEmpty(request.NationalId);
        if (nationalId is not null && await db.Users.AnyAsync(x => x.NationalId == nationalId, cancellationToken))
            throw new ConflictException("Ya existe un usuario con ese documento de identidad.");

        var roleNames = request.Roles!
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var roles = await db.Roles
            .Where(x => roleNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleNames.Length)
            throw new RequestValidationException(nameof(request.Roles), "Uno o más roles no existen.");

        var now = DateTime.UtcNow;
        var user = new User
        {
            FirstName = request.FirstName!.Trim(),
            LastName = request.LastName!.Trim(),
            Email = email,
            NationalId = nationalId,
            Phone = NullIfEmpty(request.Phone),
            BirthDate = request.BirthDate,
            IsActive = request.IsActive ?? true,
            CreatedAtUtc = now
        };

        user.PasswordHash = passwordService.Hash(user, request.Password!);

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                Role = role,
                AssignedByUserId = actorUserId,
                AssignedAtUtc = now
            });
        }

        db.Users.Add(user);
        await SqlErrorTranslator.RunAsync(() => db.SaveChangesAsync(cancellationToken));

        return new UserListItem(
            user.UserId, user.FirstName, user.LastName, user.Email, user.IsActive,
            roles.Select(r => r.Name).Order(StringComparer.Ordinal).ToArray(), user.CreatedAtUtc);
    }

    public async Task<UserListItem> SetStatusAsync(
        int userId,
        bool isActive,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (userId == actorUserId && !isActive)
            throw new ConflictException("No puedes desactivar tu propia cuenta.");

        var user = await db.Users
            .Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new ResourceNotFoundException("El usuario no existe.");

        if (!isActive && user.IsActive && user.UserRoles.Any(r => r.Role.Name == "Administrator"))
        {
            var otherActiveAdmins = await db.Users.CountAsync(
                x => x.UserId != userId && x.IsActive && x.UserRoles.Any(r => r.Role.Name == "Administrator"),
                cancellationToken);

            if (otherActiveAdmins == 0)
                throw new ConflictException("Debe existir al menos un administrador activo.");
        }

        if (user.IsActive != isActive)
        {
            user.IsActive = isActive;
            user.UpdatedAtUtc = DateTime.UtcNow;

            if (!isActive)
            {
                // A deactivated user must not be able to renew sessions.
                var now = DateTime.UtcNow;
                await db.RefreshTokens
                    .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("El usuario fue modificado por otra sesión. Recarga e inténtalo de nuevo.");
            }
        }

        return new UserListItem(
            user.UserId, user.FirstName, user.LastName, user.Email, user.IsActive,
            user.UserRoles.Select(r => r.Role.Name).Order(StringComparer.Ordinal).ToArray(), user.CreatedAtUtc);
    }

    public async Task<IReadOnlyCollection<UserLookupItem>> LookupActiveAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new UserLookupItem(x.UserId, x.FirstName + " " + x.LastName))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<RoleItem>> ListRolesAsync(CancellationToken cancellationToken) =>
        await db.Roles.AsNoTracking()
            .OrderBy(x => x.RoleId)
            .Select(x => new RoleItem(x.RoleId, x.Name, x.Description))
            .ToArrayAsync(cancellationToken);

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
