using CommunityIntranet.BuildingBlocks.Authorization;

namespace CommunityIntranet.Modules.Members.Domain;

public sealed class OrganizationInvitation
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string TokenHash { get; set; }

    public Guid CreatedByUserId { get; set; }

    public PermissionRole DefaultPermissionRole { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public int MaximumUses { get; set; }

    public int CurrentUses { get; set; }

    public bool IsRevoked { get; set; }
}
