using System.Security.Claims;
using ArchiveCore.Application.Auth;

namespace ArchiveCore.WebApi.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int? UserId =>
        int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray()
        ?? [];

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
