using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Tasks.Persistence;

public sealed class TaskCommentConfiguration
    : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.ToTable("task_comments", "tasks");
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Body).HasMaxLength(2000).IsRequired();
        builder.HasIndex(comment => new
        {
            comment.OrganizationId,
            comment.TaskId,
            comment.CreatedAt
        });
    }
}
