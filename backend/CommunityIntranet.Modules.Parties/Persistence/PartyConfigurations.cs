using CommunityIntranet.Modules.Parties.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommunityIntranet.Modules.Parties.Persistence;

public sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        builder.ToTable("parties", "parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(190).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Type).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(240);
        builder.Property(x => x.WelcomeText).HasMaxLength(1000);
        builder.Property(x => x.SpotifyProtectedRefreshToken).HasMaxLength(4000);
        builder.Property(x => x.SpotifyAccountName).HasMaxLength(200);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.OwnerUserId, x.IsArchived, x.StartAt });
    }
}

public sealed class PartyGuestConfiguration : IEntityTypeConfiguration<PartyGuest>
{
    public void Configure(EntityTypeBuilder<PartyGuest> builder)
    {
        builder.ToTable("guests", "parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SessionTokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.SessionTokenHash).IsUnique();
        builder.HasIndex(x => new { x.PartyId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.PartyId, x.LastSeenAt });
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PartyMediaConfiguration : IEntityTypeConfiguration<PartyMedia>
{
    public void Configure(EntityTypeBuilder<PartyMedia> builder)
    {
        builder.ToTable("media", "parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MediaType).HasMaxLength(16).IsRequired();
        builder.Property(x => x.StoragePath).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(240).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Caption).HasMaxLength(500);
        builder.HasIndex(x => new { x.PartyId, x.CreatedAt });
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PartyGuest>().WithMany().HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PartyMediaLikeConfiguration : IEntityTypeConfiguration<PartyMediaLike>
{
    public void Configure(EntityTypeBuilder<PartyMediaLike> builder)
    {
        builder.ToTable("media_likes", "parties");
        builder.HasKey(x => new { x.PartyMediaId, x.GuestId });
        builder.HasIndex(x => x.GuestId);
        builder.HasOne<PartyMedia>().WithMany().HasForeignKey(x => x.PartyMediaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PartyGuest>().WithMany().HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PartyOrderItemConfiguration : IEntityTypeConfiguration<PartyOrderItem>
{
    public void Configure(EntityTypeBuilder<PartyOrderItem> builder)
    {
        builder.ToTable("order_items", "parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(20);
        builder.HasIndex(x => new { x.PartyId, x.SortOrder });
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PartyOrderConfiguration : IEntityTypeConfiguration<PartyOrder>
{
    public void Configure(EntityTypeBuilder<PartyOrder> builder)
    {
        builder.ToTable("orders", "parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.CustomText).HasMaxLength(160);
        builder.HasIndex(x => x.ClaimedByGuestId);
        builder.HasIndex(x => new { x.PartyId, x.Status, x.CreatedAt });
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PartyGuest>().WithMany().HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartyGuest>().WithMany().HasForeignKey(x => x.ClaimedByGuestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PartyOrderItem>().WithMany().HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class PartyMusicRequestConfiguration : IEntityTypeConfiguration<PartyMusicRequest>
{
    public void Configure(EntityTypeBuilder<PartyMusicRequest> builder)
    {
        builder.ToTable("music_requests", "parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Song).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Artist).HasMaxLength(200);
        builder.Property(x => x.Comment).HasMaxLength(500);
        builder.Property(x => x.SpotifyTrackId).HasMaxLength(100);
        builder.Property(x => x.SpotifyUri).HasMaxLength(240);
        builder.Property(x => x.SpotifyAlbumImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(x => new { x.PartyId, x.Status, x.CreatedAt });
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PartyGuest>().WithMany().HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PartyMusicVoteConfiguration : IEntityTypeConfiguration<PartyMusicVote>
{
    public void Configure(EntityTypeBuilder<PartyMusicVote> builder)
    {
        builder.ToTable("music_votes", "parties");
        builder.HasKey(x => new { x.PartyMusicRequestId, x.GuestId });
        builder.HasIndex(x => x.GuestId);
        builder.HasOne<PartyMusicRequest>().WithMany().HasForeignKey(x => x.PartyMusicRequestId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PartyGuest>().WithMany().HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PartyGuestbookEntryConfiguration : IEntityTypeConfiguration<PartyGuestbookEntry>
{
    public void Configure(EntityTypeBuilder<PartyGuestbookEntry> builder)
    {
        builder.ToTable("guestbook_entries", "parties");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.HasIndex(x => new { x.PartyId, x.CreatedAt });
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PartyGuest>().WithMany().HasForeignKey(x => x.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
