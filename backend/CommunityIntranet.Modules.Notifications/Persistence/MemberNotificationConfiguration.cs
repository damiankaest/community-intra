using CommunityIntranet.Modules.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Notifications.Persistence;

public sealed class MemberNotificationConfiguration
    : IEntityTypeConfiguration<MemberNotification>
{
    public void Configure(EntityTypeBuilder<MemberNotification> builder)
    {
        builder.ToTable("notifications", "notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.NotificationType)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(notification => notification.Title)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(notification => notification.Body)
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(notification => notification.EntityType)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(notification => new
        {
            notification.OrganizationId,
            notification.RecipientMemberId,
            notification.ReadAt,
            notification.CreatedAt
        });
    }
}
