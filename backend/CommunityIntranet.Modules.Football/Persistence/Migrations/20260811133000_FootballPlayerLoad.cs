using CommunityIntranet.Modules.Football.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Modules.Football.Persistence.Migrations;

[DbContext(typeof(FootballDbContext))]
[Migration("20260811133000_FootballPlayerLoad")]
public sealed class FootballPlayerLoad : Migration
{
    private static readonly string[] AvailabilityMemberIndexColumns = ["OrganizationId", "MemberId"];
    private static readonly string[] SessionLoadUniqueIndexColumns = ["OrganizationId", "SessionId", "MemberId"];
    private static readonly string[] SessionLoadHistoryIndexColumns = ["OrganizationId", "MemberId", "UpdatedAt"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "player_availability",
            schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                MaxLoadPercent = table.Column<int>(type: "integer", nullable: false),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_player_availability", x => x.Id));

        migrationBuilder.CreateTable(
            name: "session_load",
            schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Rpe = table.Column<int>(type: "integer", nullable: false),
                MinutesCompleted = table.Column<int>(type: "integer", nullable: true),
                Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_session_load", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_player_availability_OrganizationId_MemberId",
            schema: "football",
            table: "player_availability",
            columns: AvailabilityMemberIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_session_load_OrganizationId_SessionId_MemberId",
            schema: "football",
            table: "session_load",
            columns: SessionLoadUniqueIndexColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_session_load_OrganizationId_MemberId_UpdatedAt",
            schema: "football",
            table: "session_load",
            columns: SessionLoadHistoryIndexColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "player_availability", schema: "football");
        migrationBuilder.DropTable(name: "session_load", schema: "football");
    }
}
