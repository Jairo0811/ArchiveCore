namespace ArchiveCore.WebApi.Security;

public sealed class RefreshCookieOptions
{
    public const string SectionName = "Auth:RefreshCookie";

    public string Name { get; set; } = "archivecore_refresh";

    /// <summary>
    /// Keep true. Browsers accept Secure cookies on http://localhost; set false only for
    /// plain-HTTP access through a non-localhost host during local testing.
    /// </summary>
    public bool Secure { get; set; } = true;

    public string Path { get; set; } = "/api/v1/auth";
}

/// <summary>
/// The refresh token lives only in an HttpOnly, Secure, SameSite=Strict cookie scoped to the
/// auth endpoints. JavaScript never sees it; the SPA keeps the short-lived access token in memory.
/// </summary>
public static class RefreshTokenCookie
{
    public static string? Read(HttpContext context, RefreshCookieOptions options) =>
        context.Request.Cookies.TryGetValue(options.Name, out var value) ? value : null;

    public static void Write(HttpContext context, RefreshCookieOptions options, string token, DateTime expiresAtUtc) =>
        context.Response.Cookies.Append(options.Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = options.Secure,
            SameSite = SameSiteMode.Strict,
            Path = options.Path,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true
        });

    public static void Clear(HttpContext context, RefreshCookieOptions options) =>
        context.Response.Cookies.Delete(options.Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = options.Secure,
            SameSite = SameSiteMode.Strict,
            Path = options.Path
        });
}
