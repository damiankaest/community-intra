using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260807170000_PartySpotify")]
public sealed class PartySpotify : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "SpotifyAccountName",
            schema: "parties",
            table: "parties",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
        migrationBuilder.AddColumn<bool>(
            name: "SpotifyAutoQueue",
            schema: "parties",
            table: "parties",
            type: "boolean",
            nullable: false,
            defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "SpotifyConnectedAt",
            schema: "parties",
            table: "parties",
            type: "timestamp with time zone",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "SpotifyProtectedRefreshToken",
            schema: "parties",
            table: "parties",
            type: "character varying(4000)",
            maxLength: 4000,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DurationMs",
            schema: "parties",
            table: "music_requests",
            type: "integer",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "SpotifyAlbumImageUrl",
            schema: "parties",
            table: "music_requests",
            type: "character varying(1000)",
            maxLength: 1000,
            nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "SpotifyQueuedAt",
            schema: "parties",
            table: "music_requests",
            type: "timestamp with time zone",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "SpotifyTrackId",
            schema: "parties",
            table: "music_requests",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "SpotifyUri",
            schema: "parties",
            table: "music_requests",
            type: "character varying(240)",
            maxLength: 240,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "music_votes",
            schema: "parties",
            columns: table => new
            {
                PartyMusicRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_music_votes", x => new { x.PartyMusicRequestId, x.GuestId });
                table.ForeignKey(
                    name: "FK_music_votes_guests_GuestId",
                    column: x => x.GuestId,
                    principalSchema: "parties",
                    principalTable: "guests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_music_votes_music_requests_PartyMusicRequestId",
                    column: x => x.PartyMusicRequestId,
                    principalSchema: "parties",
                    principalTable: "music_requests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_music_votes_GuestId",
            schema: "parties",
            table: "music_votes",
            column: "GuestId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "music_votes", schema: "parties");
        migrationBuilder.DropColumn(name: "SpotifyAccountName", schema: "parties", table: "parties");
        migrationBuilder.DropColumn(name: "SpotifyAutoQueue", schema: "parties", table: "parties");
        migrationBuilder.DropColumn(name: "SpotifyConnectedAt", schema: "parties", table: "parties");
        migrationBuilder.DropColumn(name: "SpotifyProtectedRefreshToken", schema: "parties", table: "parties");
        migrationBuilder.DropColumn(name: "DurationMs", schema: "parties", table: "music_requests");
        migrationBuilder.DropColumn(name: "SpotifyAlbumImageUrl", schema: "parties", table: "music_requests");
        migrationBuilder.DropColumn(name: "SpotifyQueuedAt", schema: "parties", table: "music_requests");
        migrationBuilder.DropColumn(name: "SpotifyTrackId", schema: "parties", table: "music_requests");
        migrationBuilder.DropColumn(name: "SpotifyUri", schema: "parties", table: "music_requests");
    }
}
