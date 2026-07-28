using CommunityIntranet.Modules.Projects.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Projects;

public static class DependencyInjection
{
    public static IServiceCollection AddProjectsModule(
        this IServiceCollection services)
    {
        services.AddScoped<IProjectLookup, ProjectLookup>();
        return services;
    }
}
