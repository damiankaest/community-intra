using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.Modules.ActivityFeed.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.ActivityFeed;

public static class DependencyInjection
{
    public static IServiceCollection AddActivityFeedModule(
        this IServiceCollection services)
    {
        services.AddScoped<IActivityWriter, ActivityWriter>();
        return services;
    }
}
