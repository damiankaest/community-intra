using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Tasks;

public static class DependencyInjection
{
    public static IServiceCollection AddTasksModule(
        this IServiceCollection services) =>
        services;
}
