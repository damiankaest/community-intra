using CommunityIntranet.Modules.FactoryInsights.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.FactoryInsights.Persistence;

public sealed class FactorySiteConfiguration
    : IEntityTypeConfiguration<FactorySite>
{
    public void Configure(EntityTypeBuilder<FactorySite> builder)
    {
        builder.ToTable("factory_sites", "factory_insights");
        builder.HasKey(factory => factory.Id);
        builder.Property(factory => factory.Name).HasMaxLength(120);
        builder.Property(factory => factory.Description).HasMaxLength(500);
        builder.Property(factory => factory.ConcurrencyToken)
            .IsConcurrencyToken();
        builder.HasIndex(factory => new
        {
            factory.OrganizationId,
            factory.Name
        });
    }
}
