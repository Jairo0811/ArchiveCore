using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Documents;

namespace ArchiveCore.WebApi.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        group.MapGet("/record/{recordId:long}", async (
            long recordId,
            IDocumentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListByRecordAsync(recordId, cancellationToken)));

        group.MapGet("/{documentId:long}", async (
            long documentId,
            IDocumentService service,
            CancellationToken cancellationToken) =>
        {
            var document = await service.GetAsync(documentId, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        });

        group.MapPost("/", async (
            CreateDocumentRequest request,
            IDocumentService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            try
            {
                var document = await service.CreateAsync(request, actorId, cancellationToken);
                return Results.Created($"/api/v1/documents/{document.DocumentId}", document);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{documentId:long}/versions", async (
            long documentId,
            IFormFile file,
            IDocumentService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            if (file.Length == 0)
                return Results.BadRequest(new { error = "File is empty." });

            await using var stream = file.OpenReadStream();

            try
            {
                var document = await service.AddVersionAsync(
                    documentId,
                    new AddDocumentVersionRequest(file.FileName, file.ContentType, file.Length),
                    stream,
                    actorId,
                    cancellationToken);

                return Results.Ok(document);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).DisableAntiforgery();

        group.MapDelete("/{documentId:long}", async (
            long documentId,
            IDocumentService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (currentUser.UserId is not int actorId)
                return Results.Unauthorized();

            return await service.DeleteAsync(documentId, actorId, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound();
        });

        return app;
    }
}
