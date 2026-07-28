using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Awards;

public static class DependencyInjection
{
    public static IServiceCollection AddAwardsModule(
        this IServiceCollection services) =>
        services;
}
