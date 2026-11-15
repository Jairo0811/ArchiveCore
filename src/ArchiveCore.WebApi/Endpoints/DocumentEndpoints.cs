using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Common;
using ArchiveCore.Application.Documents;
using ArchiveCore.WebApi.Http;
using ArchiveCore.WebApi.Security;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ArchiveCore.WebApi.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var configuration = app.ServiceProvider.GetRequiredService<IConfiguration>();
        var requestLimit = UploadLimits.RequestLimit(configuration);

        var group = app.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .RequireAuthorization(AuthorizationPolicies.Readers);

        group.MapGet("/categories", async (
            IDocumentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListCategoriesAsync(cancellationToken)));

        group.MapGet("/record/{recordId:long}", async (
            long recordId,
            IDocumentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.ListByRecordAsync(recordId, cancellationToken)));

        group.MapGet("/{documentId:long}", async (
            long documentId,
            IDocumentService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(documentId, cancellationToken)));

        // Streams the current (or a specific) version. The physical path is never exposed;
        // Content-Disposition carries a sanitized name (RFC 6266 / RFC 5987 encoding).
        group.MapGet("/{documentId:long}/download", async (
            long documentId,
            int? version,
            IDocumentService service,
            ICurrentUser currentUser,
            HttpContext http,
            CancellationToken cancellationToken) =>
        {
            if (version is <= 0)
                throw new RequestValidationException(nameof(version), "Debe ser un número de versión positivo.");

            var download = await service.OpenDownloadAsync(
                documentId, version, currentUser.RequireActorId(), cancellationToken);

            http.Response.Headers[HeaderNames.CacheControl] = "no-store";
            http.Response.Headers["X-Content-SHA256"] = download.Sha256Hash;

            return Results.Stream(
                download.Content,
                contentType: download.ContentType,
                fileDownloadName: download.FileName,
                enableRangeProcessing: false);
        });

        group.MapPost("/", async (
            CreateDocumentRequest request,
            IDocumentService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var document = await service.CreateAsync(request, currentUser.RequireActorId(), cancellationToken);
            return Results.Created($"/api/v1/documents/{document.DocumentId}", document);
        })
        .RequireAuthorization(AuthorizationPolicies.RecordsManagers);

        // Bearer-token API: no cookie authentication is accepted here, so antiforgery is not required.
        group.MapPost("/{documentId:long}/versions", async (
            long documentId,
            HttpRequest request,
            IDocumentService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
                throw new RequestValidationException("File", "Envía el archivo como multipart/form-data en el campo 'file'.");

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault()
                ?? throw new RequestValidationException("File", "Selecciona un archivo para subir.");

            await using var stream = file.OpenReadStream();

            var document = await service.AddVersionAsync(
                documentId,
                new UploadedFile(file.FileName, file.ContentType, file.Length, stream),
                currentUser.RequireActorId(),
                cancellationToken);

            return Results.Ok(document);
        })
        .RequireAuthorization(AuthorizationPolicies.RecordsManagers)
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitAttribute(requestLimit))
        .WithMetadata(new RequestFormLimitsAttribute { MultipartBodyLengthLimit = requestLimit });

        // Logical delete: the row and its versions stay for audit; the document disappears from listings.
        group.MapDelete("/{documentId:long}", async (
            long documentId,
            IDocumentService service,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(documentId, currentUser.RequireActorId(), cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(AuthorizationPolicies.RecordsManagers);

        return app;
    }
}
