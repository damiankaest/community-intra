namespace CommunityIntranet.Modules.Notifications.Contracts;

public sealed record NotificationResponse(
    Guid Id,
    string NotificationType,
    string Title,
    string Body,
    string EntityType,
    Guid EntityId,
    Guid? ActorMemberId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationSummaryResponse(int UnreadCount);
