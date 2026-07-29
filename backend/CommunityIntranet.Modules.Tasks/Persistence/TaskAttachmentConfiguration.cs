using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Tasks.Persistence;

public sealed class TaskAttachmentConfiguration
    : IEntityTypeConfiguration<TaskAttachment>
{
    public void Configure(EntityTypeBuilder<TaskAttachment> builder)
    {
        builder.ToTable("task_attachments", "tasks");
        builder.HasKey(attachment => attachment.Id);
        builder.Property(attachment => attachment.FileName)
            .HasMaxLength(240)
            .IsRequired();
        builder.Property(attachment => attachment.MediaType)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(attachment => attachment.Content).IsRequired();
        builder.HasIndex(attachment => new
        {
            attachment.OrganizationId,
            attachment.TaskId,
            attachment.CreatedAt
        });
    }
}
