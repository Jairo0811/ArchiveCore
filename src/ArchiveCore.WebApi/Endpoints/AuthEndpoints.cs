using ArchiveCore.Application.Auth;

namespace ArchiveCore.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService auth,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.LoginAsync(
                request,
                http.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("/refresh", async (
            RefreshRequest request,
            IAuthService auth,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            var result = await auth.RefreshAsync(
                request,
                http.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return result is null ? Results.Unauthorized() : Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("/revoke", async (
            RefreshRequest request,
            IAuthService auth,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            await auth.RevokeAsync(
                request.RefreshToken,
                http.Connection.RemoteIpAddress?.ToString(),
                cancellationToken);

            return Results.NoContent();
        }).RequireAuthorization();

        group.MapPost("/users", async (
            CreateUserRequest request,
            IAuthService auth,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            try
            {
                var user = await auth.CreateUserAsync(
                    request,
                    actorId,
                    cancellationToken);

                return Results.Created($"/api/v1/users/{user.UserId}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization("AdministratorsOnly");

        return app;
    }
}
