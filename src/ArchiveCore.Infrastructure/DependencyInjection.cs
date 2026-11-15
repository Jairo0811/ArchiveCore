using ArchiveCore.Application.Academic;
using ArchiveCore.Application.Auditing;
using ArchiveCore.Application.Auth;
using ArchiveCore.Application.Documents;
using ArchiveCore.Application.Records;
using ArchiveCore.Application.Users;
using ArchiveCore.Application.Workflow;
using ArchiveCore.Infrastructure.Academic;
using ArchiveCore.Infrastructure.Auditing;
using ArchiveCore.Infrastructure.Documents;
using ArchiveCore.Infrastructure.Persistence;
using ArchiveCore.Infrastructure.Records;
using ArchiveCore.Infrastructure.Security;
using ArchiveCore.Infrastructure.Users;
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
        services.AddScoped<AuditSessionContextInterceptor>();

        // Configuration is resolved lazily so hosts/tests can supply it after registration.
        services.AddDbContext<ArchiveCoreDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString("ArchiveCore");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'ArchiveCore' was not configured.");

            options
                .UseSqlServer(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditSessionContextInterceptor>());
        });

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                o => !string.IsNullOrWhiteSpace(o.SigningKey) && o.SigningKey.Length >= 32,
                "Jwt:SigningKey must be configured (user-secrets or environment variable) and be at least 32 characters.")
            .Validate(o => o.AccessTokenMinutes is >= 1 and <= 120, "Jwt:AccessTokenMinutes must be between 1 and 120.")
            .Validate(o => o.RefreshTokenDays is >= 1 and <= 30, "Jwt:RefreshTokenDays must be between 1 and 30.")
            .ValidateOnStart();

        services.Configure<BootstrapAdminOptions>(
            configuration.GetSection(BootstrapAdminOptions.SectionName));

        services.AddOptions<DocumentStorageOptions>()
            .Bind(configuration.GetSection(DocumentStorageOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.RootPath), "DocumentStorage:RootPath is required.")
            .Validate(o => o.MaxFileSizeBytes is > 0 and <= 512L * 1024 * 1024,
                "DocumentStorage:MaxFileSizeBytes must be between 1 byte and 512 MB.")
            .ValidateOnStart();

        services.AddScoped<PasswordService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<AdminBootstrapper>();

        services.AddScoped<IRecordService, RecordService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddSingleton<LocalFileStorage>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<ISqlLabService, SqlLabService>();

        return services;
    }
}
