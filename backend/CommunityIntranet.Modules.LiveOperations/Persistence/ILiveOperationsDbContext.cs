using CommunityIntranet.Modules.LiveOperations.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.LiveOperations.Persistence;

public interface ILiveOperationsDbContext
{
    DbSet<GameServerConnection> GameServerConnections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
