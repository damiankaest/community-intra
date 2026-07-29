using CommunityIntranet.Modules.AiAssistant.Domain;
using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Projects.Domain;
using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.AiAssistant.Persistence;

public interface IAiAssistantDbContext
{
    DbSet<WorkPlanDraft> WorkPlanDrafts { get; }

    DbSet<AssistantConversation> AssistantConversations { get; }

    DbSet<AssistantMessage> AssistantMessages { get; }

    DbSet<AssistantAction> AssistantActions { get; }

    DbSet<Project> Projects { get; }

    DbSet<WorkTask> WorkTasks { get; }

    DbSet<TaskComment> TaskComments { get; }

    DbSet<TaskAttachment> TaskAttachments { get; }

    DbSet<TaskMaterialItem> TaskMaterialItems { get; }

    DbSet<OrganizationMember> OrganizationMembers { get; }

    DbSet<ApplicationUser> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
