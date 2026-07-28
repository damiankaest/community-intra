using CommunityIntranet.Modules.Organizations.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Organizations.Persistence;

public interface IOrganizationDbContext
{
    DbSet<Organization> Organizations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
