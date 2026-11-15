using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace ArchiveCore.Api.Tests.Integration;

public sealed record RecordBody(
    long RecordId, string RecordNumber, string Title, string? Description, short RecordStatusId,
    string StatusCode, string Status, bool IsFinal, int CreatedByUserId, string CreatedByName,
    int? AssignedToUserId, string? AssignedToName, string RowVersion);

public sealed record MovementBody(
    long RecordMovementId, string MovementTypeCode, int? FromUserId, int? ToUserId, int PerformedByUserId, string? Notes);

public sealed record VersionBody(
    long DocumentVersionId, int VersionNumber, string OriginalFileName, string? MimeType,
    long FileSizeBytes, string Sha256Hash, int UploadedByUserId, bool IsCurrent);

public sealed record DocumentBody(
    long DocumentId, long RecordId, string Title, int CurrentVersion, string? CurrentFileName, VersionBody[] Versions);

public sealed record PagedBody<T>(T[] Items, int Page, int PageSize, int TotalCount);

public sealed record AuditBody(
    long AuditEventId, int? UserId, string? UserName, string EntityName, string EntityKey, string ActionType, Guid? CorrelationId);

public static class ApiFlow
{
    public static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes(
        "%PDF-1.4\n1 0 obj<< /Type /Catalog >>endobj\ntrailer<< /Root 1 0 R >>\n%%EOF\n");

    public static string UniqueNumber(string prefix = "EXP") => $"{prefix}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

    public static async Task<RecordBody> CreateRecordAsync(this TestUser user, int? assignedTo = null, string? number = null)
    {
        var response = await user.Client.PostAsJsonAsync("/api/v1/records", new
        {
            recordNumber = number ?? UniqueNumber(),
            title = "Expediente de prueba",
            description = "Creado por pruebas de integración",
            assignedToUserId = assignedTo
        }, IntegrationContext.Json);

        await response.EnsureStatusAsync(HttpStatusCode.Created);
        return await response.ReadAsync<RecordBody>();
    }

    public static async Task<DocumentBody> CreateDocumentAsync(this TestUser user, long recordId)
    {
        var response = await user.Client.PostAsJsonAsync("/api/v1/documents", new
        {
            recordId,
            documentCategoryId = 1,
            title = "Documento de prueba"
        }, IntegrationContext.Json);

        await response.EnsureStatusAsync(HttpStatusCode.Created);
        return await response.ReadAsync<DocumentBody>();
    }

    public static Task<HttpResponseMessage> UploadAsync(
        this TestUser user, long documentId, byte[] content, string fileName, string contentType = "application/pdf")
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return user.Client.PostAsync($"/api/v1/documents/{documentId}/versions", form);
    }

    public static async Task<MovementBody[]> HistoryAsync(this TestUser user, long recordId)
    {
        var response = await user.Client.GetAsync($"/api/v1/records/{recordId}/workflow/history");
        await response.EnsureStatusAsync(HttpStatusCode.OK);
        return await response.ReadAsync<MovementBody[]>();
    }

    public static async Task<AuditBody[]> AuditAsync(this TestUser admin, string entityName, string entityKey)
    {
        var response = await admin.Client.GetAsync(
            $"/api/v1/audit?entityName={entityName}&entityKey={Uri.EscapeDataString(entityKey)}&pageSize=200");
        await response.EnsureStatusAsync(HttpStatusCode.OK);
        return (await response.ReadAsync<PagedBody<AuditBody>>()).Items;
    }
}
