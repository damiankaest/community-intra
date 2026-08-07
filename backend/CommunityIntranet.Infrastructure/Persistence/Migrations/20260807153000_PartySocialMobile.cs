using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260807153000_PartySocialMobile")]
public sealed class PartySocialMobile : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ClaimedAt",
            schema: "parties",
            table: "orders",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ClaimedByGuestId",
            schema: "parties",
            table: "orders",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_orders_ClaimedByGuestId",
            schema: "parties",
            table: "orders",
            column: "ClaimedByGuestId");

        migrationBuilder.AddForeignKey(
            name: "FK_orders_guests_ClaimedByGuestId",
            schema: "parties",
            table: "orders",
            column: "ClaimedByGuestId",
            principalSchema: "parties",
            principalTable: "guests",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_orders_guests_ClaimedByGuestId",
            schema: "parties",
            table: "orders");

        migrationBuilder.DropIndex(
            name: "IX_orders_ClaimedByGuestId",
            schema: "parties",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "ClaimedAt",
            schema: "parties",
            table: "orders");

        migrationBuilder.DropColumn(
            name: "ClaimedByGuestId",
            schema: "parties",
            table: "orders");
    }
}
