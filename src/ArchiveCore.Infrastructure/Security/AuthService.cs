using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArchiveCore.Application.Auth;
using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArchiveCore.Infrastructure.Security;

public sealed class AuthService(
    ArchiveCoreDbContext db,
    PasswordService passwordService,
    IOptions<JwtOptions> jwtOptions,
    ICurrentUser currentUser,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResult?> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        request.Validate();

        var normalizedEmail = request.Email!.Trim().ToLowerInvariant();

        // Users.Email uses a case-insensitive collation, so this comparison is sargable.
        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            // Spend comparable time to reduce user-enumeration timing signals.
            passwordService.Verify(DummyUser, DummyHash, request.Password!);
            logger.LogInformation("Login rejected: unknown account");
            return null;
        }

        if (!passwordService.Verify(user, user.PasswordHash, request.Password!) || !user.IsActive)
        {
            logger.LogInformation("Login rejected for user {UserId}", user.UserId);
            return null;
        }

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    private void AddSessionAudit(int userId, string actionType, string? ipAddress) =>
        db.AuditEvents.Add(new AuditEvent
        {
            UserId = userId,
            EntityName = "Users",
            EntityKey = userId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ActionType = actionType,
            IpAddress = Truncate(ipAddress),
            CorrelationId = currentUser.CorrelationId,
            CreatedAtUtc = DateTime.UtcNow
        });

    public async Task<AuthResult?> RefreshAsync(
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 512)
            return null;

        var tokenHash = TokenUtilities.HashToken(refreshToken);

        var storedToken = await db.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null ||
            storedToken.RevokedAtUtc is not null ||
            storedToken.ExpiresAtUtc <= DateTime.UtcNow ||
            !storedToken.User.IsActive)
        {
            logger.LogInformation("Refresh rejected (unknown, expired, revoked or inactive)");
            return null;
        }

        var now = DateTime.UtcNow;
        var rawReplacement = TokenUtilities.CreateRefreshToken();
        var replacement = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = TokenUtilities.HashToken(rawReplacement),
            ExpiresAtUtc = now.AddDays(_jwt.RefreshTokenDays),
            CreatedAtUtc = now,
            CreatedByIp = Truncate(ipAddress)
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Revoke-first with a guarded update so two concurrent refreshes cannot both rotate the same token.
        var revoked = await db.RefreshTokens
            .Where(x => x.RefreshTokenId == storedToken.RefreshTokenId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevokedByIp, Truncate(ipAddress)), cancellationToken);

        if (revoked == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);

        await db.RefreshTokens
            .Where(x => x.RefreshTokenId == storedToken.RefreshTokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReplacedByTokenId, replacement.RefreshTokenId), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return BuildResult(storedToken.User, rawReplacement, replacement.ExpiresAtUtc);
    }

    public async Task RevokeAsync(
        string? refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 512)
            return;

        var tokenHash = TokenUtilities.HashToken(refreshToken);
        var now = DateTime.UtcNow;

        var userId = await db.RefreshTokens
            .Where(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null)
            .Select(x => (int?)x.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (userId is null)
            return;

        var revoked = await db.RefreshTokens
            .Where(x => x.TokenHash == tokenHash && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.RevokedByIp, Truncate(ipAddress)), cancellationToken);

        if (revoked > 0)
        {
            AddSessionAudit(userId.Value, "LOGOUT", ipAddress);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthResult> IssueTokensAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var rawRefreshToken = TokenUtilities.CreateRefreshToken();
        var now = DateTime.UtcNow;
        var refreshExpires = now.AddDays(_jwt.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = TokenUtilities.HashToken(rawRefreshToken),
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = now,
            CreatedByIp = Truncate(ipAddress)
        });

        AddSessionAudit(user.UserId, "LOGIN", ipAddress);
        await db.SaveChangesAsync(cancellationToken);

        return BuildResult(user, rawRefreshToken, refreshExpires);
    }

    private AuthResult BuildResult(User user, string rawRefreshToken, DateTime refreshExpiresAtUtc)
    {
        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_jwt.AccessTokenMinutes);
        var roles = user.UserRoles.Select(x => x.Role.Name).Distinct(StringComparer.Ordinal).ToArray();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        claims.AddRange(roles.Select(role => new Claim(JwtClaimNames.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthResult(
            accessToken,
            accessExpires,
            rawRefreshToken,
            refreshExpiresAtUtc,
            new UserSummary(user.UserId, user.FirstName, user.LastName, user.Email, roles));
    }

    private static string? Truncate(string? ip) =>
        ip is null ? null : ip.Length <= 45 ? ip : ip[..45];

    private static readonly User DummyUser = new() { Email = "dummy@archivecore.invalid" };
    private static readonly string DummyHash = new PasswordService().Hash(DummyUser, Guid.NewGuid().ToString("N"));
}

public static class JwtClaimNames
{
    public const string Role = "role";
    public const string Subject = JwtRegisteredClaimNames.Sub;
    public const string Email = JwtRegisteredClaimNames.Email;
}
