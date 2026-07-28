using CommunityIntranet.Modules.ThemePacks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.ThemePacks.Persistence;

public sealed class ThemePackEntityConfiguration
    : IEntityTypeConfiguration<ThemePack>
{
    public void Configure(EntityTypeBuilder<ThemePack> builder)
    {
        builder.ToTable("theme_packs", "theme_packs");
        builder.HasKey(themePack => themePack.Id);
        builder.Property(themePack => themePack.Key)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(themePack => themePack.Name)
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(themePack => themePack.Description)
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(themePack => themePack.Version)
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(themePack => themePack.Author)
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(themePack => themePack.ConfigurationJson)
            .HasColumnType("jsonb")
            .IsRequired();
        builder.HasIndex(themePack => new { themePack.Key, themePack.Version })
            .IsUnique();
        builder.HasIndex(themePack => themePack.IsSystemTheme);
    }
}
