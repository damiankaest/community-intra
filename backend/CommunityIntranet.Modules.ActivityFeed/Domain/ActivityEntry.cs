namespace CommunityIntranet.Modules.ActivityFeed.Domain;

public sealed class ActivityEntry
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string ActivityType { get; set; }

    public Guid ActorMemberId { get; set; }

    public required string EntityType { get; set; }

    public Guid EntityId { get; set; }

    public required string DataJson { get; set; }

    public int EventVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
