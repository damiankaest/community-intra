using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCommunityIntranetInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSql");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:PostgreSql is required. Copy .env.example to .env and use dev/start.ps1.");
        }

        services.AddDbContext<CommunityIntranetDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(CommunityIntranetDbContext).Assembly.FullName)));

        return services;
    }
}
