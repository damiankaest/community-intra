using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260807193000_PartyAdminGuestSessions")]
public sealed class PartyAdminGuestSessions : Migration
{
    private static readonly string[] AdminGuestLookupColumns =
        ["PartyId", "UserId"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsRemoved",
            schema: "parties",
            table: "guests",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "UserId",
            schema: "parties",
            table: "guests",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_guests_PartyId_UserId",
            schema: "parties",
            table: "guests",
            columns: AdminGuestLookupColumns,
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_guests_PartyId_UserId",
            schema: "parties",
            table: "guests");

        migrationBuilder.DropColumn(
            name: "IsRemoved",
            schema: "parties",
            table: "guests");

        migrationBuilder.DropColumn(
            name: "UserId",
            schema: "parties",
            table: "guests");
    }
}
