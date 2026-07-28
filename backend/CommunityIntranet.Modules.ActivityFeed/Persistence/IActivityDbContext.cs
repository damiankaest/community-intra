using CommunityIntranet.Modules.ActivityFeed.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.ActivityFeed.Persistence;

public interface IActivityDbContext
{
    DbSet<ActivityEntry> Activities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
