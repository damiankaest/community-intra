using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Organizations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Members.Persistence;

public sealed class OrganizationMemberConfiguration
    : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.ToTable("organization_members", "members");
        builder.HasKey(member => member.Id);
        builder.Property(member => member.PermissionRole)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(member => member.VisibleTitle).HasMaxLength(100);
        builder.Property(member => member.StatusMessage).HasMaxLength(280);
        builder.HasIndex(member => new { member.OrganizationId, member.UserId })
            .IsUnique();
        builder.HasIndex(member => new { member.UserId, member.IsActive });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(member => member.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
