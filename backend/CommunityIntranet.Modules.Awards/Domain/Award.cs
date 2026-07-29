namespace CommunityIntranet.Modules.Awards.Domain;

public sealed class Award
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public Guid AwardedToMemberId { get; set; }

    public Guid AwardedByMemberId { get; set; }

    public DateTimeOffset AwardedAt { get; set; }

    public required string Icon { get; set; }

    public required string Category { get; set; }

    public bool IsPublic { get; set; }
}
