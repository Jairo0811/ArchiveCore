using ArchiveCore.Infrastructure.Persistence;
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

        return services;
    }
}
