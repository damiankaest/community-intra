using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729210000_Phase13TimeClock")]
public sealed class Phase13TimeClock : Migration
{
    private static readonly string[] ShiftLookupColumns =
        ["OrganizationId", "MemberId", "EndedAt"];

    private static readonly string[] ActiveShiftColumns =
        ["OrganizationId", "MemberId"];

    private static readonly string[] LogLookupColumns =
        ["OrganizationId", "CreatedAt"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "time_tracking");

        migrationBuilder.CreateTable(
            name: "work_shifts",
            schema: "time_tracking",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                MemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                StartedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                EndedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
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
                table.PrimaryKey("PK_work_shifts", shift => shift.Id);
                table.ForeignKey(
                    name: "FK_work_shifts_organization_members_MemberId",
                    column: shift => shift.MemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_work_shifts_organizations_OrganizationId",
                    column: shift => shift.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "work_log_entries",
            schema: "time_tracking",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                MemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                WorkShiftId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Kind = table.Column<string>(
                    type: "character varying(24)",
                    maxLength: 24,
                    nullable: false),
                Note = table.Column<string>(
                    type: "character varying(240)",
                    maxLength: 240,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_work_log_entries", entry => entry.Id);
                table.ForeignKey(
                    name: "FK_work_log_entries_organization_members_MemberId",
                    column: entry => entry.MemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_work_log_entries_organizations_OrganizationId",
                    column: entry => entry.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_work_log_entries_work_shifts_WorkShiftId",
                    column: entry => entry.WorkShiftId,
                    principalSchema: "time_tracking",
                    principalTable: "work_shifts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_work_shifts_MemberId",
            schema: "time_tracking",
            table: "work_shifts",
            column: "MemberId");
        migrationBuilder.CreateIndex(
            name: "IX_work_shifts_OrganizationId_MemberId_EndedAt",
            schema: "time_tracking",
            table: "work_shifts",
            columns: ShiftLookupColumns);
        migrationBuilder.CreateIndex(
            name: "IX_work_shifts_OrganizationId_MemberId",
            schema: "time_tracking",
            table: "work_shifts",
            columns: ActiveShiftColumns,
            unique: true,
            filter: "\"EndedAt\" IS NULL");
        migrationBuilder.CreateIndex(
            name: "IX_work_log_entries_MemberId",
            schema: "time_tracking",
            table: "work_log_entries",
            column: "MemberId");
        migrationBuilder.CreateIndex(
            name: "IX_work_log_entries_WorkShiftId",
            schema: "time_tracking",
            table: "work_log_entries",
            column: "WorkShiftId");
        migrationBuilder.CreateIndex(
            name: "IX_work_log_entries_OrganizationId_CreatedAt",
            schema: "time_tracking",
            table: "work_log_entries",
            columns: LogLookupColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "work_log_entries",
            schema: "time_tracking");
        migrationBuilder.DropTable(
            name: "work_shifts",
            schema: "time_tracking");
    }
}
