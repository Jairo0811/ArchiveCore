using ArchiveCore.Application;
using ArchiveCore.Infrastructure;
using ArchiveCore.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ArchiveCoreDbContext>("archivecore-db");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    service = "ArchiveCore.WebApi",
    status = "running"
}));

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
