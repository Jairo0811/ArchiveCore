using Microsoft.AspNetCore.Mvc;

namespace ArchiveCore.WebApi.Http;

public static class ApiProblems
{
    public static IResult Unauthorized(string detail) =>
        Results.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "No autenticado",
            Detail = detail
        });
}
