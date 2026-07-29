namespace CommunityIntranet.Modules.Notifications.Domain;

public sealed class MemberNotification
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid RecipientMemberId { get; set; }

    public Guid? ActorMemberId { get; set; }

    public required string NotificationType { get; set; }

    public required string Title { get; set; }

    public required string Body { get; set; }

    public required string EntityType { get; set; }

    public Guid EntityId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ReadAt { get; set; }
}
