namespace CommunityIntranet.BuildingBlocks.Notifications;

public interface INotificationWriter
{
    void Add(NotificationDraft notification);
}

public sealed record NotificationDraft(
    Guid OrganizationId,
    Guid RecipientMemberId,
    Guid? ActorMemberId,
    string NotificationType,
    string Title,
    string Body,
    string EntityType,
    Guid EntityId);
