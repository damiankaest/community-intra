using CommunityIntranet.Modules.ThemePacks.Configuration;
using CommunityIntranet.Modules.ThemePacks.Seeding;
using CommunityIntranet.Modules.ThemePacks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.ThemePacks;

public static class DependencyInjection
{
    public static IServiceCollection AddThemePacksModule(
        this IServiceCollection services)
    {
        services.AddSingleton<ThemePackSerializer>();
        services.AddScoped<IThemePackCatalog, ThemePackCatalog>();
        services.AddScoped<ThemePackSeeder>();

        return services;
    }
}
