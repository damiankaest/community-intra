using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Members.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Members.Persistence;

public interface IMemberDbContext
{
    DbSet<OrganizationMember> OrganizationMembers { get; }

    DbSet<Department> Departments { get; }

    DbSet<OrganizationInvitation> OrganizationInvitations { get; }

    DbSet<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
