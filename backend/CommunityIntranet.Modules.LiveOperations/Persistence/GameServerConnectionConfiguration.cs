using CommunityIntranet.Modules.LiveOperations.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.LiveOperations.Persistence;

public sealed class GameServerConnectionConfiguration
    : IEntityTypeConfiguration<GameServerConnection>
{
    public void Configure(EntityTypeBuilder<GameServerConnection> builder)
    {
        builder.ToTable("game_server_connections", "live_operations");
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.DisplayName)
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(connection => connection.Host)
            .HasMaxLength(253)
            .IsRequired();
        builder.Property(connection => connection.ProtectedApiToken)
            .HasMaxLength(12000)
            .IsRequired();
        builder.Property(connection => connection.CertificateFingerprint)
            .HasMaxLength(64);
        builder.Property(connection => connection.ConcurrencyToken)
            .IsConcurrencyToken();
        builder.HasIndex(connection => connection.OrganizationId).IsUnique();
    }
}
