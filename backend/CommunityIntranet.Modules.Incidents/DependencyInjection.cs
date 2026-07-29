using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Incidents;

public static class DependencyInjection
{
    public static IServiceCollection AddIncidentsModule(
        this IServiceCollection services) =>
        services;
}
