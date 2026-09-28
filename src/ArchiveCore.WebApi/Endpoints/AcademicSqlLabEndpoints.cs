using ArchiveCore.Application.Academic;

namespace ArchiveCore.WebApi.Endpoints;

public static class AcademicSqlLabEndpoints
{
    public static IEndpointRouteBuilder MapAcademicSqlLabEndpoints(
        this IEndpointRouteBuilder app)
    {
        var lab = app.MapGroup("/api/v1/academic/sql-lab")
            .WithTags("Academic SQL Lab")
            .RequireAuthorization("AdministratorsOnly");

        lab.MapGet("/queries", (ISqlLabService service) =>
            Results.Ok(service.GetQueries()));

        lab.MapGet("/queries/{key}", async (
            string key,
            ISqlLabService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ExecuteAsync(key, cancellationToken);
            return result is null
                ? Results.NotFound(new { message = "Academic query was not found." })
                : Results.Ok(result);
        });

        return app;
    }
}
