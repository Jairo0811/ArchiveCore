using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Common;
using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchiveCore.Infrastructure.Security;

public sealed class AdminBootstrapper(
    ArchiveCoreDbContext db,
    PasswordService passwordService,
    IOptions<BootstrapAdminOptions> options,
    ILogger<AdminBootstrapper> logger)
{
    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        var config = options.Value;

        if (!config.Enabled)
            return;

        if (await db.Users.AnyAsync(cancellationToken))
            return;

        // Same rules as administrator-provisioned users (PasswordPolicy).
        if (string.IsNullOrWhiteSpace(config.Email) || !RequestValidator.IsValidEmail(config.Email.Trim()))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Email must be a valid email address.");
        }

        if (!PasswordPolicy.IsValid(config.Password))
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:Password does not satisfy the password policy: " + PasswordPolicy.Description);
        }

        if (string.IsNullOrWhiteSpace(config.FirstName) || string.IsNullOrWhiteSpace(config.LastName) ||
            config.FirstName.Trim().Length > 100 || config.LastName.Trim().Length > 100)
        {
            throw new InvalidOperationException(
                "BootstrapAdmin:FirstName and BootstrapAdmin:LastName are required (max 100 characters).");
        }

        var adminRole = await db.Roles
            .SingleOrDefaultAsync(x => x.Name == "Administrator", cancellationToken)
            ?? throw new InvalidOperationException(
                "Administrator role was not found. Run database seed scripts first.");

        var user = new User
        {
            FirstName = config.FirstName.Trim(),
            LastName = config.LastName.Trim(),
            Email = config.Email.Trim().ToLowerInvariant(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordService.Hash(user, config.Password);

        user.UserRoles.Add(new UserRole
        {
            Role = adminRole,
            AssignedAtUtc = DateTime.UtcNow
        });

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Bootstrap administrator created (UserId {UserId}). Disable BootstrapAdmin and remove its password from configuration.",
            user.UserId);
    }
}
