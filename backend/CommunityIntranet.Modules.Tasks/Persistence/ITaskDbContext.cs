using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Tasks.Persistence;

public interface ITaskDbContext
{
    DbSet<WorkTask> WorkTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
