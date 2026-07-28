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

    Task<bool> IsActiveMemberAsync(
        Guid organizationId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<string?> GetMemberDisplayNameAsync(
        Guid organizationId,
        Guid memberId,
        CancellationToken cancellationToken);
}

public sealed record OrganizationMembership(
    Guid MemberId,
    Guid OrganizationId,
    Guid UserId,
    PermissionRole PermissionRole,
    string? VisibleTitle);
