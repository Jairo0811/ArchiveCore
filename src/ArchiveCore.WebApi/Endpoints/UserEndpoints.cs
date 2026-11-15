using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Users;
using ArchiveCore.WebApi.Http;
using ArchiveCore.WebApi.Security;

namespace ArchiveCore.WebApi.Endpoints;

/// <summary>Minimal administrator-only user provisioning.</summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization(AuthorizationPolicies.Administrators);

        group.MapGet("/", async (
            IUserAdminService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(cancellationToken)));

        group.MapGet("/roles", async (
            IUserAdminService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListRolesAsync(cancellationToken)));

        group.MapPost("/", async (
            CreateUserRequest request,
            IUserAdminService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var user = await service.CreateAsync(request, currentUser.RequireActorId(), cancellationToken);
            return Results.Created($"/api/v1/users/{user.UserId}", user);
        });

        group.MapPatch("/{userId:int}/status", async (
            int userId,
            UpdateUserStatusRequest request,
            IUserAdminService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            request.Validate();
            return Results.Ok(await service.SetStatusAsync(
                userId, request.IsActive!.Value, currentUser.RequireActorId(), cancellationToken));
        });

        return app;
    }
}
