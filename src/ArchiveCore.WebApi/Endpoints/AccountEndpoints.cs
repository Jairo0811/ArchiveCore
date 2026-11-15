using ArchiveCore.Application.Auth;
using ArchiveCore.WebApi.Security;

namespace ArchiveCore.WebApi.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/account/me", (ICurrentUser currentUser) =>
            Results.Ok(new
            {
                currentUser.UserId,
                currentUser.Email,
                currentUser.Roles
            }))
        .WithTags("Account")
        .RequireAuthorization(AuthorizationPolicies.Readers);

        return app;
    }
}
