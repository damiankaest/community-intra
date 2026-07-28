using CommunityIntranet.BuildingBlocks.Authorization;

namespace CommunityIntranet.BuildingBlocks.Tenancy;

public interface IOrganizationAccessService
{
    Task<OrganizationMembership?> GetActiveMembershipAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<OrganizationMembership>> GetActiveMembershipsAsync(
        Guid userId,
        CancellationToken cancellationToken);
}

public sealed record OrganizationMembership(
    Guid OrganizationId,
    PermissionRole PermissionRole,
    string? VisibleTitle);
