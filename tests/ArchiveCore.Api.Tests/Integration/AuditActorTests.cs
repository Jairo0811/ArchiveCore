using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using ArchiveCore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ArchiveCore.Api.Tests.Integration;

[Collection(IntegrationCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuditActorTests(IntegrationContext ctx)
{
    [Fact]
    public async Task Update_by_another_user_is_attributed_to_that_user()
    {
        // User A creates the record.
        var record = await ctx.ManagerA.CreateRecordAsync();

        // User B modifies it through EF Core (PUT) and through a stored procedure (transfer).
        var update = await ctx.ManagerB.Client.PutAsJsonAsync($"/api/v1/records/{record.RecordId}",
            new { title = "Modificado por B", recordStatusId = record.RecordStatusId, rowVersion = record.RowVersion },
            IntegrationContext.Json);
        await update.EnsureStatusAsync(HttpStatusCode.OK);
        var correlationId = Guid.Parse(update.Headers.GetValues("X-Correlation-Id").Single());

        await (await ctx.ManagerB.Client.PostAsJsonAsync($"/api/v1/records/{record.RecordId}/workflow/transfer",
            new { toUserId = ctx.ManagerA.UserId }, IntegrationContext.Json)).EnsureStatusAsync(HttpStatusCode.NoContent);

        var events = await ctx.Admin.AuditAsync("Records", record.RecordId.ToString());

        var insert = Assert.Single(events, x => x.ActionType == "INSERT");
        Assert.Equal(ctx.ManagerA.UserId, insert.UserId);

        var updates = events.Where(x => x.ActionType == "UPDATE").ToArray();
        Assert.Equal(2, updates.Length);
        Assert.All(updates, x => Assert.Equal(ctx.ManagerB.UserId, x.UserId));
        Assert.Contains(updates, x => x.CorrelationId == correlationId);
        Assert.DoesNotContain(events, x => x.UserId is null);
    }

    [Fact]
    public async Task Session_context_never_leaks_between_requests_on_pooled_connections()
    {
        static async Task<int?> ReadActorAsync(IServiceProvider root, ClaimsPrincipal user)
        {
            await using var scope = root.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext =
                new DefaultHttpContext { User = user, RequestServices = scope.ServiceProvider };

            var db = scope.ServiceProvider.GetRequiredService<ArchiveCoreDbContext>();
            return await db.Database
                .SqlQueryRaw<int?>("SELECT TRY_CONVERT(INT, SESSION_CONTEXT(N'UserId')) AS [Value]")
                .SingleAsync();
        }

        var authenticated = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", ctx.ManagerA.UserId.ToString()), new Claim("role", "RecordsManager")], "Test"));
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(ctx.ManagerA.UserId, await ReadActorAsync(ctx.Factory.Services, authenticated));
            Assert.Null(await ReadActorAsync(ctx.Factory.Services, anonymous));
        }
    }

    [Fact]
    public async Task Audit_filters_by_user_action_and_date_range()
    {
        var record = await ctx.ManagerB.CreateRecordAsync();
        var from = DateTime.UtcNow.AddMinutes(-5).ToString("O");
        var to = DateTime.UtcNow.AddMinutes(5).ToString("O");

        var response = await ctx.Admin.Client.GetAsync(
            $"/api/v1/audit?entityName=Records&userId={ctx.ManagerB.UserId}&actionType=insert&fromUtc={Uri.EscapeDataString(from)}&toUtc={Uri.EscapeDataString(to)}");
        await response.EnsureStatusAsync(HttpStatusCode.OK);
        var page = await response.ReadAsync<PagedBody<AuditBody>>();

        Assert.Contains(page.Items, x => x.EntityKey == record.RecordId.ToString());
        Assert.All(page.Items, x => Assert.Equal("INSERT", x.ActionType));
        Assert.DoesNotContain("oldValuesJson", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var invalid = await ctx.Admin.Client.GetAsync("/api/v1/audit?actionType=DROP");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }
}
