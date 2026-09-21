using ArchiveCore.Application.Auth;
using ArchiveCore.Infrastructure.Persistence;
using ArchiveCore.Infrastructure.Security;
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

        services.AddScoped<PasswordService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
