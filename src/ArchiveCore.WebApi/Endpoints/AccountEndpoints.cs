using ArchiveCore.Application.Auth;

namespace ArchiveCore.WebApi.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/account/me", (ICurrentUser currentUser) =>
        {
            if (!currentUser.IsAuthenticated)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                currentUser.UserId,
                currentUser.Email,
                currentUser.Roles
            });
        })
        .WithTags("Account")
        .RequireAuthorization();

        return app;
    }
}
