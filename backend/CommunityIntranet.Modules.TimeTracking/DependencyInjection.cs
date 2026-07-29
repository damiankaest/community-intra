using CommunityIntranet.Modules.TimeTracking.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.TimeTracking;

public static class DependencyInjection
{
    public static IServiceCollection AddTimeTrackingModule(
        this IServiceCollection services)
    {
        services.AddScoped<ITimeClockService, TimeClockService>();
        return services;
    }
}
