using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Tasks.Persistence;

public interface ITaskDbContext
{
    DbSet<WorkTask> WorkTasks { get; }

    DbSet<TaskComment> TaskComments { get; }

    DbSet<TaskAttachment> TaskAttachments { get; }

    DbSet<TaskMaterialItem> TaskMaterialItems { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
