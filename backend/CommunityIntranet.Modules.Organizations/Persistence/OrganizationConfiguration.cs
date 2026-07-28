using CommunityIntranet.Modules.Organizations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Organizations.Persistence;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations", "organizations");
        builder.HasKey(organization => organization.Id);
        builder.Property(organization => organization.Name)
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(organization => organization.Slug)
            .HasMaxLength(140)
            .IsRequired();
        builder.Property(organization => organization.Description)
            .HasMaxLength(1000);
        builder.Property(organization => organization.Language)
            .HasMaxLength(10)
            .IsRequired();
        builder.Property(organization => organization.TimeZone)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(organization => organization.Slug).IsUnique();
        builder.HasIndex(organization => organization.OwnerUserId);
    }
}
