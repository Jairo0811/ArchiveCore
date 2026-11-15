using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DocumentTests(IntegrationContext ctx)
{
    [Fact]
    public async Task Versions_hash_and_download_round_trip()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);
        Assert.Equal(0, document.CurrentVersion);

        var v1Bytes = ApiFlow.PdfBytes;
        var v2Bytes = Encoding.ASCII.GetBytes("%PDF-1.7\n% segunda versión\n%%EOF\n");

        var v1 = await ctx.ManagerA.UploadAsync(document.DocumentId, v1Bytes, "informe.pdf");
        await v1.EnsureStatusAsync(HttpStatusCode.OK);
        var afterV1 = await v1.ReadAsync<DocumentBody>();
        Assert.Equal(1, afterV1.CurrentVersion);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(v1Bytes)), afterV1.Versions.Single().Sha256Hash.Trim());
        Assert.Equal("application/pdf", afterV1.Versions.Single().MimeType);

        // A different user uploads the second version.
        var v2 = await ctx.ManagerB.UploadAsync(document.DocumentId, v2Bytes, "informe-v2.pdf", "application/octet-stream");
        await v2.EnsureStatusAsync(HttpStatusCode.OK);
        var afterV2 = await v2.ReadAsync<DocumentBody>();
        Assert.Equal(2, afterV2.CurrentVersion);
        Assert.Single(afterV2.Versions, x => x.IsCurrent);
        Assert.Equal(ctx.ManagerB.UserId, afterV2.Versions.Single(x => x.IsCurrent).UploadedByUserId);

        // Any reader can download; content, type and file name are served safely.
        var current = await ctx.Reader.Client.GetAsync($"/api/v1/documents/{document.DocumentId}/download");
        await current.EnsureStatusAsync(HttpStatusCode.OK);
        Assert.Equal(v2Bytes, await current.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/pdf", current.Content.Headers.ContentType?.MediaType);
        Assert.Equal("informe-v2.pdf", current.Content.Headers.ContentDisposition?.FileNameStar
            ?? current.Content.Headers.ContentDisposition?.FileName?.Trim('"'));
        Assert.Equal("nosniff", current.Headers.GetValues("X-Content-Type-Options").Single());

        var first = await ctx.Reader.Client.GetAsync($"/api/v1/documents/{document.DocumentId}/download?version=1");
        await first.EnsureStatusAsync(HttpStatusCode.OK);
        Assert.Equal(v1Bytes, await first.Content.ReadAsByteArrayAsync());

        var missing = await ctx.Reader.Client.GetAsync($"/api/v1/documents/{document.DocumentId}/download?version=9");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var versionKey = afterV2.Versions.Single(x => x.IsCurrent).DocumentVersionId.ToString();
        var audit = await ctx.Admin.AuditAsync("DocumentVersions", versionKey);
        Assert.Contains(audit, x => x.ActionType == "DOWNLOAD" && x.UserId == ctx.Reader.UserId);
        Assert.Contains(audit, x => x.ActionType == "INSERT" && x.UserId == ctx.ManagerB.UserId);

        // The physical path never leaves the server.
        var json = await (await ctx.Reader.Client.GetAsync($"/api/v1/documents/{document.DocumentId}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("storagePath", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(ctx.StorageRoot, json);
    }

    [Theory]
    [InlineData("malware.exe", "MZ")]
    [InlineData("disfrazado.pdf", "MZ\u0090\u0000binary")]
    [InlineData("vacio.pdf", "")]
    public async Task Disallowed_or_spoofed_files_are_rejected_without_orphans(string fileName, string content)
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);
        var before = CountStoredFiles();

        var response = await ctx.ManagerA.UploadAsync(document.DocumentId, Encoding.Latin1.GetBytes(content), fileName);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, CountStoredFiles());
    }

    [Fact]
    public async Task Oversized_upload_is_rejected()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);
        var payload = new byte[3 * 1024 * 1024];
        ApiFlow.PdfBytes.CopyTo(payload, 0);

        var response = await ctx.ManagerA.UploadAsync(document.DocumentId, payload, "grande.pdf");

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge,
            $"Unexpected status {response.StatusCode}");
    }

    [Fact]
    public async Task Traversal_file_names_are_sanitized()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);

        var response = await ctx.ManagerA.UploadAsync(document.DocumentId, ApiFlow.PdfBytes, "../../../etc/escape.pdf");
        await response.EnsureStatusAsync(HttpStatusCode.OK);

        var body = await response.ReadAsync<DocumentBody>();
        Assert.Equal("escape.pdf", body.CurrentFileName);
        Assert.All(Directory.EnumerateFiles(ctx.StorageRoot, "*", SearchOption.AllDirectories),
            path => Assert.StartsWith(Path.GetFullPath(ctx.StorageRoot), Path.GetFullPath(path)));
    }

    [Fact]
    public async Task Concurrent_uploads_produce_sequential_versions_and_one_current()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);

        var uploads = Enumerable.Range(1, 5)
            .Select(i => (i % 2 == 0 ? ctx.ManagerA : ctx.ManagerB)
                .UploadAsync(document.DocumentId, ApiFlow.PdfBytes, $"paralelo-{i}.pdf"))
            .ToArray();

        foreach (var response in await Task.WhenAll(uploads))
            await response.EnsureStatusAsync(HttpStatusCode.OK);

        await using var scope = ctx.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ArchiveCoreDbContext>();
        var versions = await db.DocumentVersions.AsNoTracking()
            .Where(x => x.DocumentId == document.DocumentId)
            .OrderBy(x => x.VersionNumber)
            .ToListAsync();

        Assert.Equal([1, 2, 3, 4, 5], versions.Select(x => x.VersionNumber));
        Assert.Single(versions, x => x.IsCurrent);
        Assert.Equal(5, versions.Single(x => x.IsCurrent).VersionNumber);
    }

    [Fact]
    public async Task Soft_delete_hides_the_document_but_keeps_the_row()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);
        await (await ctx.ManagerA.UploadAsync(document.DocumentId, ApiFlow.PdfBytes, "a-borrar.pdf")).EnsureStatusAsync(HttpStatusCode.OK);

        var forbidden = await ctx.Reader.Client.DeleteAsync($"/api/v1/documents/{document.DocumentId}");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        await (await ctx.ManagerB.Client.DeleteAsync($"/api/v1/documents/{document.DocumentId}")).EnsureStatusAsync(HttpStatusCode.NoContent);

        Assert.Equal(HttpStatusCode.NotFound, (await ctx.Reader.Client.GetAsync($"/api/v1/documents/{document.DocumentId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ctx.Reader.Client.GetAsync($"/api/v1/documents/{document.DocumentId}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ctx.ManagerB.Client.DeleteAsync($"/api/v1/documents/{document.DocumentId}")).StatusCode);

        var list = await (await ctx.Reader.Client.GetAsync($"/api/v1/documents/record/{record.RecordId}")).ReadAsync<DocumentBody[]>();
        Assert.DoesNotContain(list, x => x.DocumentId == document.DocumentId);

        await using var scope = ctx.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ArchiveCoreDbContext>();
        var row = await db.Documents.AsNoTracking().SingleAsync(x => x.DocumentId == document.DocumentId);
        Assert.True(row.IsDeleted);

        var audit = await ctx.Admin.AuditAsync("Documents", document.DocumentId.ToString());
        Assert.Contains(audit, x => x.ActionType == "UPDATE" && x.UserId == ctx.ManagerB.UserId);
    }

    [Fact]
    public async Task Closed_records_do_not_accept_new_versions_or_documents()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var document = await ctx.ManagerA.CreateDocumentAsync(record.RecordId);
        await (await ctx.ManagerA.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/close",
            new { }, IntegrationContext.Json)).EnsureStatusAsync(HttpStatusCode.NoContent);

        var before = CountStoredFiles();
        var upload = await ctx.ManagerA.UploadAsync(document.DocumentId, ApiFlow.PdfBytes, "tarde.pdf");
        Assert.Equal(HttpStatusCode.Conflict, upload.StatusCode);
        Assert.Equal(before, CountStoredFiles());

        var create = await ctx.ManagerA.Client.PostAsJsonAsync("/api/v1/documents",
            new { recordId = record.RecordId, documentCategoryId = 1, title = "Fuera de plazo" }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);
    }

    [Fact]
    public async Task Invalid_category_returns_400()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();
        var response = await ctx.ManagerA.Client.PostAsJsonAsync("/api/v1/documents",
            new { recordId = record.RecordId, documentCategoryId = 9999, title = "Categoría inexistente" }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private int CountStoredFiles() =>
        Directory.Exists(ctx.StorageRoot)
            ? Directory.EnumerateFiles(ctx.StorageRoot, "*", SearchOption.AllDirectories).Count()
            : 0;
}
