using CommunityIntranet.Modules.Mystery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Mystery.Persistence;

public sealed class MysterySessionConfiguration : IEntityTypeConfiguration<MysterySession>
{
    public void Configure(EntityTypeBuilder<MysterySession> builder)
    {
        builder.ToTable("sessions", "mystery");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.JoinCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(24).IsRequired();
        builder.Property(x => x.GameMaster).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Notice).HasMaxLength(500);
        builder.Property(x => x.ConfigurationJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.SecretCaseJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.GameStateJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasIndex(x => x.JoinCode).IsUnique();
        builder.HasIndex(x => new { x.Status, x.UpdatedAt });
    }
}
