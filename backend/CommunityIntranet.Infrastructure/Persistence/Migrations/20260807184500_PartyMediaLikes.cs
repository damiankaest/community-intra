using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260807184500_PartyMediaLikes")]
public sealed class PartyMediaLikes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "media_likes",
            schema: "parties",
            columns: table => new
            {
                PartyMediaId = table.Column<Guid>(type: "uuid", nullable: false),
                GuestId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_media_likes", x => new { x.PartyMediaId, x.GuestId });
                table.ForeignKey(
                    name: "FK_media_likes_guests_GuestId",
                    column: x => x.GuestId,
                    principalSchema: "parties",
                    principalTable: "guests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_media_likes_media_PartyMediaId",
                    column: x => x.PartyMediaId,
                    principalSchema: "parties",
                    principalTable: "media",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
        migrationBuilder.CreateIndex(
            name: "IX_media_likes_GuestId",
            schema: "parties",
            table: "media_likes",
            column: "GuestId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "media_likes", schema: "parties");
    }
}
