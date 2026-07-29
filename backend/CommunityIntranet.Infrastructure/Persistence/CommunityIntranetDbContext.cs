using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Identity.Persistence;
using CommunityIntranet.Modules.AiAssistant.Domain;
using CommunityIntranet.Modules.AiAssistant.Persistence;
using CommunityIntranet.Modules.ActivityFeed.Domain;
using CommunityIntranet.Modules.ActivityFeed.Persistence;
using CommunityIntranet.Modules.Awards.Domain;
using CommunityIntranet.Modules.Awards.Persistence;
using CommunityIntranet.Modules.Incidents.Domain;
using CommunityIntranet.Modules.Incidents.Persistence;
using CommunityIntranet.Modules.LiveOperations.Domain;
using CommunityIntranet.Modules.LiveOperations.Persistence;
using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Notifications.Domain;
using CommunityIntranet.Modules.Notifications.Persistence;
using CommunityIntranet.Modules.Organizations.Domain;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.Projects.Domain;
using CommunityIntranet.Modules.Projects.Persistence;
using CommunityIntranet.Modules.Tasks.Domain;
using CommunityIntranet.Modules.Tasks.Persistence;
using CommunityIntranet.Modules.ThemePacks.Domain;
using CommunityIntranet.Modules.ThemePacks.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed class CommunityIntranetDbContext(
    DbContextOptions<CommunityIntranetDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options),
        IIdentityDbContext,
        IOrganizationDbContext,
        IMemberDbContext,
        IThemePackDbContext,
        IProjectDbContext,
        ITaskDbContext,
        IIncidentDbContext,
        IAwardDbContext,
        IActivityDbContext,
        IAiAssistantDbContext,
        INotificationDbContext,
        ILiveOperationsDbContext
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<OrganizationMember> OrganizationMembers =>
        Set<OrganizationMember>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<OrganizationInvitation> OrganizationInvitations =>
        Set<OrganizationInvitation>();

    public DbSet<ThemePack> ThemePacks => Set<ThemePack>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();

    public DbSet<TaskComment> TaskComments => Set<TaskComment>();

    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<Award> Awards => Set<Award>();

    public DbSet<ActivityEntry> Activities => Set<ActivityEntry>();

    public DbSet<WorkPlanDraft> WorkPlanDrafts => Set<WorkPlanDraft>();

    public DbSet<AssistantConversation> AssistantConversations =>
        Set<AssistantConversation>();

    public DbSet<AssistantMessage> AssistantMessages => Set<AssistantMessage>();

    public DbSet<AssistantAction> AssistantActions => Set<AssistantAction>();

    public DbSet<MemberNotification> Notifications => Set<MemberNotification>();

    public DbSet<GameServerConnection> GameServerConnections =>
        Set<GameServerConnection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(CommunityIntranetDbContext).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationUser).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Organization).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(OrganizationMember).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ThemePack).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Project).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(WorkTask).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Incident).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(Award).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(ActivityEntry).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(WorkPlanDraft).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(MemberNotification).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(GameServerConnection).Assembly);

        builder.Entity<IdentityRole<Guid>>().ToTable("roles", "identity");
        builder.Entity<IdentityUserClaim<Guid>>()
            .ToTable("user_claims", "identity");
        builder.Entity<IdentityUserLogin<Guid>>()
            .ToTable("user_logins", "identity");
        builder.Entity<IdentityUserRole<Guid>>()
            .ToTable("user_roles", "identity");
        builder.Entity<IdentityRoleClaim<Guid>>()
            .ToTable("role_claims", "identity");
        builder.Entity<IdentityUserToken<Guid>>()
            .ToTable("user_tokens", "identity");

        builder.Entity<Organization>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(organization => organization.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OrganizationMember>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Organization>()
            .HasOne<ThemePack>()
            .WithMany()
            .HasForeignKey(organization => organization.ThemePackId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Project>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(project => project.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Project>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(project => project.OwnerMemberId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<WorkTask>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(task => task.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WorkTask>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<WorkTask>()
            .HasOne<WorkTask>()
            .WithMany()
            .HasForeignKey(task => task.ParentTaskId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WorkTask>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(task => task.AssignedMemberId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<WorkTask>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(task => task.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TaskComment>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(comment => comment.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TaskComment>()
            .HasOne<WorkTask>()
            .WithMany()
            .HasForeignKey(comment => comment.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TaskComment>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(comment => comment.AuthorMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TaskAttachment>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(attachment => attachment.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TaskAttachment>()
            .HasOne<WorkTask>()
            .WithMany()
            .HasForeignKey(attachment => attachment.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TaskAttachment>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(attachment => attachment.UploadedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Incident>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(incident => incident.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Incident>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(incident => incident.ReportedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Incident>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(incident => incident.ResponsibleMemberId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<Award>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(award => award.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<Award>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(award => award.AwardedToMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<Award>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(award => award.AwardedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ActivityEntry>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(activity => activity.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ActivityEntry>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(activity => activity.ActorMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkPlanDraft>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(draft => draft.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<WorkPlanDraft>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(draft => draft.CreatedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<WorkPlanDraft>()
            .HasOne<Project>()
            .WithMany()
            .HasForeignKey(draft => draft.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<AssistantConversation>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(conversation => conversation.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssistantConversation>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(conversation => conversation.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssistantMessage>()
            .HasOne<AssistantConversation>()
            .WithMany()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssistantMessage>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(message => message.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssistantMessage>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(message => message.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssistantAction>()
            .HasOne<AssistantConversation>()
            .WithMany()
            .HasForeignKey(action => action.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssistantAction>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(action => action.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<AssistantAction>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(action => action.RequestedByMemberId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<MemberNotification>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(notification => notification.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<MemberNotification>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(notification => notification.RecipientMemberId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<MemberNotification>()
            .HasOne<OrganizationMember>()
            .WithMany()
            .HasForeignKey(notification => notification.ActorMemberId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Entity<GameServerConnection>()
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(connection => connection.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
