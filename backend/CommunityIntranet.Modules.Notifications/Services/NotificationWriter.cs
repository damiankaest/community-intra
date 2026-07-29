using CommunityIntranet.BuildingBlocks.Notifications;
using CommunityIntranet.Modules.Notifications.Domain;
using CommunityIntranet.Modules.Notifications.Persistence;

namespace CommunityIntranet.Modules.Notifications.Services;

public sealed class NotificationWriter(
    INotificationDbContext dbContext,
    TimeProvider timeProvider) : INotificationWriter
{
    public void Add(NotificationDraft notification)
    {
        if (notification.ActorMemberId == notification.RecipientMemberId)
        {
            return;
        }

        dbContext.Notifications.Add(new MemberNotification
        {
            Id = Guid.NewGuid(),
            OrganizationId = notification.OrganizationId,
            RecipientMemberId = notification.RecipientMemberId,
            ActorMemberId = notification.ActorMemberId,
            NotificationType = Truncate(notification.NotificationType, 64),
            Title = Truncate(notification.Title, 200),
            Body = Truncate(notification.Body, 500),
            EntityType = Truncate(notification.EntityType, 64),
            EntityId = notification.EntityId,
            CreatedAt = timeProvider.GetUtcNow()
        });
    }

    private static string Truncate(string value, int maximumLength)
    {
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : normalized[..maximumLength];
    }
}
