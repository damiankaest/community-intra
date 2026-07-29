using CommunityIntranet.Infrastructure.Persistence;
using CommunityIntranet.Modules.AiAssistant.Persistence;
using CommunityIntranet.Modules.Identity.Persistence;
using CommunityIntranet.Modules.ActivityFeed.Persistence;
using CommunityIntranet.Modules.Awards.Persistence;
using CommunityIntranet.Modules.Incidents.Persistence;
using CommunityIntranet.Modules.LiveOperations.Persistence;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Notifications.Persistence;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.Projects.Persistence;
using CommunityIntranet.Modules.Tasks.Persistence;
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
        services.AddScoped<IProjectDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<ITaskDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<IIncidentDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<IAwardDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<IActivityDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<IAiAssistantDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<INotificationDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());
        services.AddScoped<ILiveOperationsDbContext>(
            provider => provider.GetRequiredService<CommunityIntranetDbContext>());

        return services;
    }
}
