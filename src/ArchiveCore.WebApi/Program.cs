using System.Text;
using ArchiveCore.Application;
using ArchiveCore.Application.Auth;
using ArchiveCore.Infrastructure;
using ArchiveCore.Infrastructure.Persistence;
using ArchiveCore.Infrastructure.Security;
using ArchiveCore.WebApi.Endpoints;
using ArchiveCore.WebApi.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var jwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(jwt.SigningKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey must be configured using user-secrets or an environment variable.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdministratorsOnly", policy =>
        policy.RequireRole("Administrator"));

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                    return false;

                return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
            })
            .AllowAnyHeader()
            .AllowAnyMethod());
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "ArchiveCore.WebApi",
    status = "running"
}));

app.MapAuthEndpoints();
app.MapAccountEndpoints();
app.MapRecordEndpoints();
app.MapDocumentEndpoints();
app.MapWorkflowEndpoints();
app.MapAuditEndpoints();
app.MapCatalogEndpoints();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
