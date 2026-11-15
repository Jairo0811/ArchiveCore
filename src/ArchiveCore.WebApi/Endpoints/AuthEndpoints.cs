using ArchiveCore.Application.Auth;
using ArchiveCore.WebApi.Http;
using ArchiveCore.WebApi.Security;
using Microsoft.Extensions.Options;

namespace ArchiveCore.WebApi.Endpoints;

/// <summary>
/// Session lifecycle. The refresh token travels only in an HttpOnly cookie scoped to
/// /api/v1/auth; the body carries the short-lived access token and the user profile.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication").AllowAnonymous();

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService auth,
            ICurrentUser currentUser,
            IOptions<RefreshCookieOptions> cookieOptions,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.LoginAsync(request, currentUser.IpAddress, cancellationToken);

            if (result is null)
            {
                RefreshTokenCookie.Clear(http, cookieOptions.Value);
                return ApiProblems.Unauthorized("Correo o contraseña incorrectos, o la cuenta está inactiva.");
            }

            RefreshTokenCookie.Write(http, cookieOptions.Value, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Results.Ok(ToResponse(result));
        })
        .RequireRateLimiting(RateLimitPolicies.Login);

        group.MapPost("/refresh", async (
            IAuthService auth,
            ICurrentUser currentUser,
            IOptions<RefreshCookieOptions> cookieOptions,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.RefreshAsync(
                RefreshTokenCookie.Read(http, cookieOptions.Value),
                currentUser.IpAddress,
                cancellationToken);

            if (result is null)
            {
                RefreshTokenCookie.Clear(http, cookieOptions.Value);
                return ApiProblems.Unauthorized("La sesión expiró o fue revocada. Inicia sesión de nuevo.");
            }

            RefreshTokenCookie.Write(http, cookieOptions.Value, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Results.Ok(ToResponse(result));
        })
        .RequireRateLimiting(RateLimitPolicies.Refresh);

        // Anonymous on purpose: logout must work even when the access token already expired.
        // The cookie is the credential being revoked; SameSite=Strict prevents cross-site use.
        group.MapPost("/revoke", async (
            IAuthService auth,
            ICurrentUser currentUser,
            IOptions<RefreshCookieOptions> cookieOptions,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            await auth.RevokeAsync(
                RefreshTokenCookie.Read(http, cookieOptions.Value),
                currentUser.IpAddress,
                cancellationToken);

            RefreshTokenCookie.Clear(http, cookieOptions.Value);
            return Results.NoContent();
        })
        .RequireRateLimiting(RateLimitPolicies.Refresh);

        return app;
    }

    private static AuthResponse ToResponse(AuthResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresAtUtc, result.User);
}
