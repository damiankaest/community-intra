using CommunityIntranet.Modules.Football.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Modules.Football.Persistence.Migrations;

[DbContext(typeof(FootballDbContext))]
[Migration("20260811180500_FootballTrainingCoachTasks")]
public sealed class FootballTrainingCoachTasks : Migration
{
    private static readonly string[] SortIndexColumns = ["OrganizationId", "SessionId", "TrainingBlockId", "SortOrder"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "training_coach_tasks",
            schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                TrainingBlockId = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Task = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                SortOrder = table.Column<int>(type: "integer", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_training_coach_tasks", x => x.Id);
                table.ForeignKey(
                    name: "FK_training_coach_tasks_training_blocks_TrainingBlockId",
                    column: x => x.TrainingBlockId,
                    principalSchema: "football",
                    principalTable: "training_blocks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_training_coach_tasks_TrainingBlockId",
            schema: "football",
            table: "training_coach_tasks",
            column: "TrainingBlockId");

        migrationBuilder.CreateIndex(
            name: "IX_training_coach_tasks_OrganizationId_SessionId_TrainingBlockId_SortOrder",
            schema: "football",
            table: "training_coach_tasks",
            columns: SortIndexColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "training_coach_tasks", schema: "football");
    }
}
