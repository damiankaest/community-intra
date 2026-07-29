using CommunityIntranet.Modules.Awards.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Awards.Persistence;

public sealed class AwardConfiguration : IEntityTypeConfiguration<Award>
{
    public void Configure(EntityTypeBuilder<Award> builder)
    {
        builder.ToTable("awards", "awards");
        builder.HasKey(award => award.Id);
        builder.Property(award => award.Name).HasMaxLength(160).IsRequired();
        builder.Property(award => award.Description)
            .HasMaxLength(2000)
            .IsRequired();
        builder.Property(award => award.Icon).HasMaxLength(50).IsRequired();
        builder.Property(award => award.Category)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(award => new
        {
            award.OrganizationId,
            award.AwardedAt
        });
        builder.HasIndex(award => new
        {
            award.OrganizationId,
            award.AwardedToMemberId
        });
    }
}
