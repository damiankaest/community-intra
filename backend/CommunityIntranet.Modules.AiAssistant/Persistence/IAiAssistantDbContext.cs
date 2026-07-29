using CommunityIntranet.Modules.AiAssistant.Domain;
using CommunityIntranet.Modules.Projects.Domain;
using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.AiAssistant.Persistence;

public interface IAiAssistantDbContext
{
    DbSet<WorkPlanDraft> WorkPlanDrafts { get; }

    DbSet<Project> Projects { get; }

    DbSet<WorkTask> WorkTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
