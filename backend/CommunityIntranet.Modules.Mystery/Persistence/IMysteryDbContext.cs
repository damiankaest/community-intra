using CommunityIntranet.Modules.Mystery.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Mystery.Persistence;

public interface IMysteryDbContext
{
    DbSet<MysterySession> MysterySessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
