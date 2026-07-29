using CommunityIntranet.Modules.Awards.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Awards.Persistence;

public interface IAwardDbContext
{
    DbSet<Award> Awards { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
