using CommunityIntranet.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Identity.Persistence;

public sealed class SteamIdentityConfiguration : IEntityTypeConfiguration<SteamIdentity>
{
    public void Configure(EntityTypeBuilder<SteamIdentity> builder)
    {
        builder.ToTable("steam_identities", "identity");
        builder.HasKey(identity => identity.Id);
        builder.HasIndex(identity => identity.UserId).IsUnique();
        builder.HasIndex(identity => identity.SteamId64).IsUnique();
        builder.Property(identity => identity.SteamId64).HasMaxLength(20).IsRequired();
        builder.Property(identity => identity.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(identity => identity.AvatarUrl).HasMaxLength(500);
        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<SteamIdentity>(identity => identity.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
