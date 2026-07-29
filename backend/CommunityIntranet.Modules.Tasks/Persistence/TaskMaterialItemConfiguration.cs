using CommunityIntranet.Modules.Tasks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Tasks.Persistence;

public sealed class TaskMaterialItemConfiguration
    : IEntityTypeConfiguration<TaskMaterialItem>
{
    public void Configure(EntityTypeBuilder<TaskMaterialItem> builder)
    {
        builder.ToTable("task_material_items", "tasks");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(160).IsRequired();
        builder.Property(item => item.Quantity).HasMaxLength(80).IsRequired();
        builder.Property(item => item.Notes).HasMaxLength(300);
        builder.Property(item => item.ConcurrencyToken).IsConcurrencyToken();
        builder.HasIndex(item => new { item.OrganizationId, item.TaskId });
    }
}
