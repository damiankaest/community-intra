using CommunityIntranet.Modules.FactoryInsights.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.FactoryInsights.Persistence;

public interface IFactoryInsightsDbContext
{
    DbSet<FactorySite> FactorySites { get; }

    DbSet<SaveSnapshot> SaveSnapshots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
