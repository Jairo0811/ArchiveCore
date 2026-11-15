using ArchiveCore.Application;
using ArchiveCore.Application.Auth;
using ArchiveCore.Infrastructure;
using ArchiveCore.Infrastructure.Persistence;
using ArchiveCore.Infrastructure.Security;
using ArchiveCore.WebApi.Endpoints;
using ArchiveCore.WebApi.Http;
using ArchiveCore.WebApi.Security;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddArchiveCoreAuthentication();
builder.Services.AddAuthorizationBuilder().AddArchiveCorePolicies();
builder.Services.AddArchiveCoreRateLimiting();
builder.Services.AddArchiveCoreCors(builder.Configuration, builder.Environment);
builder.Services.AddArchiveCoreForwardedHeaders(builder.Configuration);

builder.Services.AddOptions<RefreshCookieOptions>()
    .Bind(builder.Configuration.GetSection(RefreshCookieOptions.SectionName));

// Uniform RFC 7807 errors: validation, binding, auth status codes and unexpected failures.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
        if (CorrelationIdMiddleware.Get(context.HttpContext) is { } correlationId)
            context.ProblemDetails.Extensions["correlationId"] = correlationId;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Invalid JSON / missing bodies throw BadHttpRequestException, which the handler maps to 400.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.Configure<JsonOptions>(options => options.SerializerOptions.AllowTrailingCommas = false);

// Uploads: ASP.NET limits are derived from the document storage limit (see DocumentEndpoints).
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = UploadLimits.RequestLimit(builder.Configuration);
});

builder.Services.AddOpenApi();

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ArchiveCoreDbContext>("archivecore-db");

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var bootstrapper = scope.ServiceProvider.GetRequiredService<AdminBootstrapper>();
    await bootstrapper.BootstrapAsync();
}

app.UseForwardedHeaders();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Configuration.GetValue("Security:UseHttpsRedirection", !app.Environment.IsDevelopment()))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(CorsSetup.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();

app.MapGet("/", () => Results.Ok(new
{
    service = "ArchiveCore.WebApi",
    status = "running"
})).AllowAnonymous().ExcludeFromDescription();

app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapRecordEndpoints();
app.MapDocumentEndpoints();
app.MapWorkflowEndpoints();
app.MapAuditEndpoints();
app.MapCatalogEndpoints();
app.MapUserEndpoints();
app.MapAcademicSqlLabEndpoints();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program;
