using CommunityIntranet.Modules.ActivityFeed.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.ActivityFeed.Persistence;

public sealed class ActivityEntryConfiguration
    : IEntityTypeConfiguration<ActivityEntry>
{
    public void Configure(EntityTypeBuilder<ActivityEntry> builder)
    {
        builder.ToTable("activities", "activity");
        builder.HasKey(activity => activity.Id);
        builder.Property(activity => activity.ActivityType)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(activity => activity.EntityType)
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(activity => activity.DataJson)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.HasIndex(activity => new
        {
            activity.OrganizationId,
            activity.CreatedAt
        });
        builder.HasIndex(activity => new
        {
            activity.OrganizationId,
            activity.EntityType,
            activity.EntityId
        });
    }
}
