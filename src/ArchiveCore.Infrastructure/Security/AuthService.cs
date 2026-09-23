using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ArchiveCore.Application.Auth;
using ArchiveCore.Domain.Entities;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArchiveCore.Infrastructure.Security;

public sealed class AuthService(
    ArchiveCoreDbContext db,
    PasswordService passwordService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(
                x => x.Email.ToLower() == normalizedEmail && x.IsActive,
                cancellationToken);

        if (user is null || !passwordService.Verify(user, user.PasswordHash, request.Password))
            return null;

        return await IssueTokensAsync(user, ipAddress, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(
        RefreshRequest request,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenUtilities.HashToken(request.RefreshToken);

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
            return null;
        }

        var rawReplacement = TokenUtilities.CreateRefreshToken();
        var replacement = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = TokenUtilities.HashToken(rawReplacement),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress
        };

        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        storedToken.ReplacedByTokenId = replacement.RefreshTokenId;
        await db.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(storedToken.User, rawReplacement, replacement.ExpiresAtUtc);
    }

    public async Task RevokeAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var tokenHash = TokenUtilities.HashToken(refreshToken);

        var storedToken = await db.RefreshTokens
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAtUtc is not null)
            return;

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedByIp = ipAddress;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserSummary> CreateUserAsync(
        CreateUserRequest request,
        int performedByUserId,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(x => x.Email.ToLower() == normalizedEmail, cancellationToken))
            throw new InvalidOperationException("A user with that email already exists.");

        var roleNames = request.Roles
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var roles = await db.Roles
            .Where(x => roleNames.Contains(x.Name))
            .ToListAsync(cancellationToken);

        if (roles.Count != roleNames.Length)
            throw new InvalidOperationException("One or more requested roles do not exist.");

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = normalizedEmail,
            NationalId = request.NationalId?.Trim(),
            Phone = request.Phone?.Trim(),
            BirthDate = request.BirthDate,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordService.Hash(user, request.Password);

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole
            {
                Role = role,
                AssignedByUserId = performedByUserId,
                AssignedAtUtc = DateTime.UtcNow
            });
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return ToSummary(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var rawRefreshToken = TokenUtilities.CreateRefreshToken();
        var refreshExpires = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = TokenUtilities.HashToken(rawRefreshToken),
            ExpiresAtUtc = refreshExpires,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByIp = ipAddress
        });

        await db.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user, rawRefreshToken, refreshExpires);
    }

    private AuthResponse BuildAuthResponse(
        User user,
        string rawRefreshToken,
        DateTime refreshExpiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(_jwt.SigningKey) || _jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("JWT signing key must be configured and at least 32 characters.");

        var now = DateTime.UtcNow;
        var accessExpires = now.AddMinutes(_jwt.AccessTokenMinutes);
        var roles = user.UserRoles.Select(x => x.Role.Name).Distinct().ToArray();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: accessExpires,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        return new AuthResponse(
            accessToken,
            accessExpires,
            rawRefreshToken,
            refreshExpiresAtUtc,
            ToSummary(user));
    }

    private static UserSummary ToSummary(User user) =>
        new(
            user.UserId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.UserRoles.Select(x => x.Role.Name).Distinct().ToArray());
}
