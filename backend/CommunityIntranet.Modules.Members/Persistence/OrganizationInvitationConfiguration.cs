using CommunityIntranet.Modules.Members.Domain;
using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Organizations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Members.Persistence;

public sealed class OrganizationInvitationConfiguration
    : IEntityTypeConfiguration<OrganizationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        builder.ToTable("organization_invitations", "members");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.TokenHash)
            .HasMaxLength(64)
            .IsFixedLength()
            .IsRequired();
        builder.Property(invitation => invitation.DefaultPermissionRole)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(invitation => invitation.CurrentUses)
            .IsConcurrencyToken();
        builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
        builder.HasIndex(invitation => new
        {
            invitation.OrganizationId,
            invitation.IsRevoked,
            invitation.ExpiresAt
        });
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(invitation => invitation.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(invitation => invitation.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
