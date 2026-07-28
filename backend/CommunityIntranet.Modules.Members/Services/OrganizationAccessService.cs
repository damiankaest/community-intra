using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Organizations.Services;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Members.Services;

public sealed class OrganizationAccessService(
    IMemberDbContext dbContext,
    TimeProvider timeProvider)
    : IOrganizationAccessService, IOrganizationOwnerProvisioner
{
    public void AddOwner(Guid organizationId, Guid userId, string? visibleTitle)
    {
        dbContext.OrganizationMembers.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            PermissionRole = PermissionRole.Owner,
            VisibleTitle = visibleTitle,
            JoinedAt = timeProvider.GetUtcNow(),
            IsActive = true
        });
    }

    public void AddDepartments(
        Guid organizationId,
        IReadOnlyList<OrganizationDepartmentTemplate> departments)
    {
        var sortOrder = 0;
        foreach (var department in departments)
        {
            dbContext.Departments.Add(new Department
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Name = department.Name.Trim(),
                Icon = department.Icon.Trim(),
                SortOrder = sortOrder++,
                IsArchived = false
            });
        }
    }

    public Task<OrganizationMembership?> GetActiveMembershipAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.OrganizationMembers
            .AsNoTracking()
            .Where(member =>
                member.OrganizationId == organizationId
                && member.UserId == userId
                && member.IsActive)
            .Select(member => new OrganizationMembership(
                member.OrganizationId,
                member.PermissionRole,
                member.VisibleTitle))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<OrganizationMembership>>
        GetActiveMembershipsAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
        await dbContext.OrganizationMembers
            .AsNoTracking()
            .Where(member => member.UserId == userId && member.IsActive)
            .Select(member => new OrganizationMembership(
                member.OrganizationId,
                member.PermissionRole,
                member.VisibleTitle))
            .ToArrayAsync(cancellationToken);
}
