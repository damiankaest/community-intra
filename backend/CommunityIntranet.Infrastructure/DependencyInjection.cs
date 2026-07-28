using CommunityIntranet.Infrastructure.Persistence;
using CommunityIntranet.Modules.Identity.Persistence;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.ThemePacks.Persistence;
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
        services.AddScoped<IIdentityDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<IOrganizationDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<IMemberDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<IThemePackDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());

        return services;
    }
}
