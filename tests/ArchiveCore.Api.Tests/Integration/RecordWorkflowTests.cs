using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RecordWorkflowTests(IntegrationContext ctx)
{
    [Fact]
    public async Task Create_with_assignee_registers_the_initial_assignment_movement()
    {
        var record = await ctx.ManagerA.CreateRecordAsync(assignedTo: ctx.ManagerB.UserId);

        Assert.Equal("OPEN", record.StatusCode);
        Assert.Equal(ctx.ManagerA.UserId, record.CreatedByUserId);
        Assert.Equal(ctx.ManagerB.UserId, record.AssignedToUserId);
        Assert.False(string.IsNullOrWhiteSpace(record.AssignedToName));

        var history = await ctx.Reader.HistoryAsync(record.RecordId);
        var movement = Assert.Single(history);
        Assert.Equal("ASSIGN", movement.MovementTypeCode);
        Assert.Null(movement.FromUserId);
        Assert.Equal(ctx.ManagerB.UserId, movement.ToUserId);
        Assert.Equal(ctx.ManagerA.UserId, movement.PerformedByUserId);
    }

    [Fact]
    public async Task Duplicate_record_number_returns_409()
    {
        var number = ApiFlow.UniqueNumber("DUP");
        await ctx.ManagerA.CreateRecordAsync(number: number);

        var duplicate = await ctx.ManagerB.Client.PostAsJsonAsync("/api/v1/records",
            new { recordNumber = number, title = "Duplicado" }, IntegrationContext.Json);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Theory]
    [InlineData("""{"recordNumber":"","title":""}""")]
    [InlineData("""{"recordNumber":"CON ESPACIOS","title":"Válido"}""")]
    [InlineData("""{"recordNumber":"EXP-1","title":"Válido","assignedToUserId":-4}""")]
    [InlineData("""{}""")]
    public async Task Invalid_record_payloads_return_400_with_field_errors(string json)
    {
        var response = await ctx.ManagerA.Client.PostAsync("/api/v1/records",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"errors\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Assigning_to_unknown_or_inactive_user_returns_400()
    {
        var unknown = await ctx.ManagerA.Client.PostAsJsonAsync("/api/v1/records",
            new { recordNumber = ApiFlow.UniqueNumber(), title = "Asignación inválida", assignedToUserId = 999999 },
            IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        var inactive = await ctx.CreateUserAsync("Usuario", "Desactivado", "User");
        await (await ctx.Admin.Client.PatchAsJsonAsync($"/api/v1/users/{inactive.UserId}/status",
            new { isActive = false }, IntegrationContext.Json)).EnsureStatusAsync(HttpStatusCode.OK);

        var record = await ctx.ManagerA.CreateRecordAsync();
        var transfer = await ctx.ManagerA.Client.PostAsJsonAsync(
            $"/api/v1/records/{record.RecordId}/workflow/transfer", new { toUserId = inactive.UserId }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.BadRequest, transfer.StatusCode);
    }

    [Fact]
    public async Task Assign_then_transfer_then_close_keeps_a_coherent_history()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();

        await (await ctx.ManagerA.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/transfer",
            new { toUserId = ctx.ManagerB.UserId, notes = "Asignación" }, IntegrationContext.Json))
            .EnsureStatusAsync(HttpStatusCode.NoContent);

        var sameUser = await ctx.ManagerA.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/transfer",
            new { toUserId = ctx.ManagerB.UserId }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Conflict, sameUser.StatusCode);

        await (await ctx.ManagerB.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/transfer",
            new { toUserId = ctx.Reader.UserId, notes = "Traslado" }, IntegrationContext.Json))
            .EnsureStatusAsync(HttpStatusCode.NoContent);

        await (await ctx.ManagerB.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/close",
            new { notes = "Cierre" }, IntegrationContext.Json))
            .EnsureStatusAsync(HttpStatusCode.NoContent);

        var closedAgain = await ctx.ManagerA.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/close",
            new { }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Conflict, closedAgain.StatusCode);

        var transferClosed = await ctx.ManagerA.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/transfer",
            new { toUserId = ctx.ManagerA.UserId }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Conflict, transferClosed.StatusCode);

        var details = await (await ctx.Reader.Client.GetAsync($"/api/v1/records/{record.RecordId}")).ReadAsync<RecordBody>();
        Assert.True(details.IsFinal);
        Assert.Equal("CLOSED", details.StatusCode);

        var updateClosed = await ctx.ManagerA.Client.PutAsJsonAsync($"/api/v1/records/{record.RecordId}",
            new { title = "Cambio", recordStatusId = details.RecordStatusId, rowVersion = details.RowVersion }, IntegrationContext.Json);
        Assert.Equal(HttpStatusCode.Conflict, updateClosed.StatusCode);

        var history = (await ctx.Reader.HistoryAsync(record.RecordId)).Reverse().ToArray();
        Assert.Equal(["ASSIGN", "TRANSFER", "CLOSE"], history.Select(x => x.MovementTypeCode));
        Assert.Equal(ctx.ManagerB.UserId, history[1].FromUserId);
        Assert.Equal(ctx.Reader.UserId, history[1].ToUserId);
        Assert.Equal(ctx.ManagerB.UserId, history[2].PerformedByUserId);
    }

    [Fact]
    public async Task Stale_rowversion_returns_409()
    {
        var record = await ctx.ManagerA.CreateRecordAsync();

        await (await ctx.ManagerA.Client.PutAsJsonAsync($"/api/v1/records/{record.RecordId}",
            new { title = "Primera edición", recordStatusId = record.RecordStatusId, rowVersion = record.RowVersion },
            IntegrationContext.Json)).EnsureStatusAsync(HttpStatusCode.OK);

        var stale = await ctx.ManagerB.Client.PutAsJsonAsync($"/api/v1/records/{record.RecordId}",
            new { title = "Edición con versión vieja", recordStatusId = record.RecordStatusId, rowVersion = record.RowVersion },
            IntegrationContext.Json);

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Search_filters_and_pagination_are_bounded()
    {
        var number = ApiFlow.UniqueNumber("SRCH");
        await ctx.ManagerA.CreateRecordAsync(assignedTo: ctx.ManagerB.UserId, number: number);

        var found = await (await ctx.Reader.Client.GetAsync($"/api/v1/records?search={number}")).ReadAsync<PagedBody<RecordBody>>();
        Assert.Equal(1, found.TotalCount);

        var byAssignee = await (await ctx.Reader.Client.GetAsync(
            $"/api/v1/records?search={number}&assignedToUserId={ctx.ManagerA.UserId}")).ReadAsync<PagedBody<RecordBody>>();
        Assert.Equal(0, byAssignee.TotalCount);

        var paged = await (await ctx.Reader.Client.GetAsync("/api/v1/records?pageSize=100000")).ReadAsync<PagedBody<RecordBody>>();
        Assert.True(paged.PageSize <= 100);
    }

    [Fact]
    public async Task Unknown_record_returns_404()
    {
        var response = await ctx.Reader.Client.GetAsync("/api/v1/records/987654321");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
