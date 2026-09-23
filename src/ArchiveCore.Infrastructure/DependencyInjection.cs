using ArchiveCore.Application.Auditing;
using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Documents;
using ArchiveCore.Application.Records;
using ArchiveCore.Application.Workflow;
using ArchiveCore.Infrastructure.Auditing;
using ArchiveCore.Infrastructure.Documents;
using ArchiveCore.Infrastructure.Persistence;
using ArchiveCore.Infrastructure.Records;
using ArchiveCore.Infrastructure.Security;
using ArchiveCore.Infrastructure.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArchiveCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ArchiveCore")
            ?? throw new InvalidOperationException(
                "Connection string 'ArchiveCore' was not configured.");

        services.AddDbContext<ArchiveCoreDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services.Configure<BootstrapAdminOptions>(
            configuration.GetSection(BootstrapAdminOptions.SectionName));

        services.AddScoped<PasswordService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AdminBootstrapper>();

        services.AddScoped<IRecordService, RecordService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<LocalFileStorage>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
