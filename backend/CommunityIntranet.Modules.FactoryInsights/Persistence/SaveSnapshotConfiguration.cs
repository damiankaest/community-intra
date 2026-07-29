using CommunityIntranet.Modules.FactoryInsights.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.FactoryInsights.Persistence;

public sealed class SaveSnapshotConfiguration
    : IEntityTypeConfiguration<SaveSnapshot>
{
    public void Configure(EntityTypeBuilder<SaveSnapshot> builder)
    {
        builder.ToTable("save_snapshots", "factory_insights");
        builder.HasKey(snapshot => snapshot.Id);
        builder.Property(snapshot => snapshot.OriginalFileName)
            .HasMaxLength(180);
        builder.Property(snapshot => snapshot.ContentSha256)
            .HasMaxLength(64);
        builder.Property(snapshot => snapshot.SaveName).HasMaxLength(180);
        builder.Property(snapshot => snapshot.SessionName).HasMaxLength(180);
        builder.Property(snapshot => snapshot.MapName).HasMaxLength(180);
        builder.Property(snapshot => snapshot.ParserVersion).HasMaxLength(32);
        builder.Property(snapshot => snapshot.Source)
            .HasConversion<string>()
            .HasMaxLength(24);
        builder.Property(snapshot => snapshot.AnalysisJson)
            .HasColumnType("jsonb");
        builder.HasIndex(snapshot => new
        {
            snapshot.OrganizationId,
            snapshot.ImportedAt
        });
        builder.HasIndex(snapshot => new
            {
                snapshot.OrganizationId,
                snapshot.ContentSha256
            })
            .IsUnique();
    }
}
