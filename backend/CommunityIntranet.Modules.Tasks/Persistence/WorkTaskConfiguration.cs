using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Tasks.Persistence;

public sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("tasks", "tasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Title).HasMaxLength(200).IsRequired();
        builder.Property(task => task.Description).HasMaxLength(4000);
        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(task => task.Priority)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(task => task.ConcurrencyToken).IsConcurrencyToken();
        builder.HasIndex(task => new { task.OrganizationId, task.Status });
        builder.HasIndex(task => new
        {
            task.OrganizationId,
            task.AssignedMemberId
        });
        builder.HasIndex(task => new { task.OrganizationId, task.ProjectId });
        builder.HasIndex(task => new { task.OrganizationId, task.ParentTaskId });
    }
}
