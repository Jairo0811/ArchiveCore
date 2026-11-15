using ArchiveCore.Application.Auth;
using ArchiveCore.Infrastructure.Security;
using ArchiveCore.WebApi.Http;

namespace ArchiveCore.WebApi.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private HttpContext? Context => accessor.HttpContext;

    public int? UserId =>
        int.TryParse(Context?.User.FindFirst(JwtClaimNames.Subject)?.Value, out var id) ? id : null;

    public string? Email => Context?.User.FindFirst(JwtClaimNames.Email)?.Value;

    public IReadOnlyCollection<string> Roles =>
        Context?.User.FindAll(JwtClaimNames.Role).Select(x => x.Value).ToArray() ?? [];

    public bool IsAuthenticated => Context?.User.Identity?.IsAuthenticated == true;

    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    public Guid? CorrelationId => Context is null ? null : CorrelationIdMiddleware.Get(Context);
}
