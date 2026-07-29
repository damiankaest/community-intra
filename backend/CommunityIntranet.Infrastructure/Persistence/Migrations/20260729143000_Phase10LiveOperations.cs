using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729143000_Phase10LiveOperations")]
public partial class Phase10LiveOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "live_operations");

        migrationBuilder.CreateTable(
            name: "game_server_connections",
            schema: "live_operations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                DisplayName = table.Column<string>(
                    type: "character varying(120)",
                    maxLength: 120,
                    nullable: false),
                Host = table.Column<string>(
                    type: "character varying(253)",
                    maxLength: 253,
                    nullable: false),
                Port = table.Column<int>(type: "integer", nullable: false),
                ProtectedApiToken = table.Column<string>(
                    type: "character varying(12000)",
                    maxLength: 12000,
                    nullable: false),
                CertificateFingerprint = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: true),
                IsEnabled = table.Column<bool>(
                    type: "boolean",
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ConcurrencyToken = table.Column<Guid>(
                    type: "uuid",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_game_server_connections", item => item.Id);
                table.ForeignKey(
                    name:
                        "FK_game_server_connections_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_game_server_connections_OrganizationId",
            schema: "live_operations",
            table: "game_server_connections",
            column: "OrganizationId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "game_server_connections",
            schema: "live_operations");
    }
}
