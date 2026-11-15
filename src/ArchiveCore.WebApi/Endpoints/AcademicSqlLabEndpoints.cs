using ArchiveCore.Application.Academic;
using ArchiveCore.Application.Common;
using ArchiveCore.WebApi.Security;

namespace ArchiveCore.WebApi.Endpoints;

/// <summary>
/// SOF-008 SQL Lab: Administrator only, predefined read-only queries. No user-supplied SQL
/// is ever executed; the route parameter only selects one of the server-side definitions.
/// </summary>
public static class AcademicSqlLabEndpoints
{
    public static IEndpointRouteBuilder MapAcademicSqlLabEndpoints(this IEndpointRouteBuilder app)
    {
        var lab = app.MapGroup("/api/v1/academic/sql-lab")
            .WithTags("Academic SQL Lab")
            .RequireAuthorization(AuthorizationPolicies.Administrators);

        lab.MapGet("/queries", (ISqlLabService service) =>
            Results.Ok(service.GetQueries()));

        lab.MapGet("/queries/{key}", async (
            string key,
            ISqlLabService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ExecuteAsync(key, cancellationToken)
                ?? throw new ResourceNotFoundException("La consulta académica no existe.")));

        return app;
    }
}
