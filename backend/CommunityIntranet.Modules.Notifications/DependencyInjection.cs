using CommunityIntranet.BuildingBlocks.Notifications;
using CommunityIntranet.Modules.Notifications.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CommunityIntranet.Modules.Notifications;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsModule(
        this IServiceCollection services)
    {
        services.AddScoped<INotificationWriter, NotificationWriter>();
        return services;
    }
}
