namespace CommunityIntranet.BuildingBlocks.ActivityFeed;

public sealed record ActivityDraft(
    Guid OrganizationId,
    string ActivityType,
    Guid ActorMemberId,
    string EntityType,
    Guid EntityId,
    IReadOnlyDictionary<string, string?> Data);
