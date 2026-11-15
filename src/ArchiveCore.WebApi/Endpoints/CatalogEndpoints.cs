using ArchiveCore.Application.Documents;
using ArchiveCore.Application.Records;
using ArchiveCore.Application.Users;
using ArchiveCore.WebApi.Security;

namespace ArchiveCore.WebApi.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalogs")
            .WithTags("Catalogs")
            .RequireAuthorization(AuthorizationPolicies.Readers);

        group.MapGet("/record-statuses", async (
            IRecordService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListStatusesAsync(cancellationToken)));

        group.MapGet("/document-categories", async (
            IDocumentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListCategoriesAsync(cancellationToken)));

        // Minimal projection (id + full name) for responsible/assignee pickers and filters.
        group.MapGet("/users", async (
            IUserAdminService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.LookupActiveAsync(cancellationToken)));

        return app;
    }
}
