using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ArchiveCore.WebApi.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalogs")
            .WithTags("Catalogs")
            .RequireAuthorization();

        group.MapGet("/record-statuses", async (
            ArchiveCoreDbContext db,
            CancellationToken cancellationToken) =>
            Results.Ok(await db.RecordStatuses.AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .Select(x => new { x.RecordStatusId, x.Code, x.Name, x.IsFinal })
                .ToArrayAsync(cancellationToken)));

        group.MapGet("/document-categories", async (
            ArchiveCoreDbContext db,
            CancellationToken cancellationToken) =>
            Results.Ok(await db.DocumentCategories.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new { x.DocumentCategoryId, x.Code, x.Name })
                .ToArrayAsync(cancellationToken)));

        group.MapGet("/users", async (
            ArchiveCoreDbContext db,
            CancellationToken cancellationToken) =>
            Results.Ok(await db.Users.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.LastName)
                .ThenBy(x => x.FirstName)
                .Select(x => new { x.UserId, x.FirstName, x.LastName, x.Email })
                .ToArrayAsync(cancellationToken)));

        return app;
    }
}
