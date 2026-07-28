using CommunityIntranet.Modules.Members.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Members.Persistence;

public interface IMemberDbContext
{
    DbSet<OrganizationMember> OrganizationMembers { get; }
}
