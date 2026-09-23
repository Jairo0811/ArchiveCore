using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ArchiveCore.Infrastructure.Security;

public sealed class AdminBootstrapper(
    ArchiveCoreDbContext db,
    PasswordService passwordService,
    IOptions<BootstrapAdminOptions> options)
{
    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        var config = options.Value;

        if (!config.Enabled)
            return;

        if (await db.Users.AnyAsync(cancellationToken))
            return;

        if (string.IsNullOrWhiteSpace(config.Email) ||
            string.IsNullOrWhiteSpace(config.Password) ||
            config.Password.Length < 12)
        {
            throw new InvalidOperationException(
                "Bootstrap admin requires a valid email and a password of at least 12 characters.");
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
    }
}
