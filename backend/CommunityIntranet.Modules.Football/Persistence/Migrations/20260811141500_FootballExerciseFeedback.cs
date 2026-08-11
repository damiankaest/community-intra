using CommunityIntranet.Modules.Football.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Modules.Football.Persistence.Migrations;

[DbContext(typeof(FootballDbContext))]
[Migration("20260811141500_FootballExerciseFeedback")]
public sealed class FootballExerciseFeedback : Migration
{
    private static readonly string[] ExerciseHistoryIndexColumns = ["OrganizationId", "ExerciseId", "UpdatedAt"];
    private static readonly string[] TrainingBlockFeedbackUniqueIndexColumns = ["OrganizationId", "SessionId", "TrainingBlockId", "MemberId"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "exercise_feedback",
            schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                TrainingBlockId = table.Column<Guid>(type: "uuid", nullable: false),
                ExerciseId = table.Column<Guid>(type: "uuid", nullable: true),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Fun = table.Column<int>(type: "integer", nullable: false),
                Difficulty = table.Column<int>(type: "integer", nullable: false),
                Benefit = table.Column<int>(type: "integer", nullable: false),
                Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_exercise_feedback", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_exercise_feedback_OrganizationId_ExerciseId_UpdatedAt",
            schema: "football",
            table: "exercise_feedback",
            columns: ExerciseHistoryIndexColumns);

        migrationBuilder.CreateIndex(
            name: "IX_exercise_feedback_OrganizationId_SessionId_TrainingBlockId_MemberId",
            schema: "football",
            table: "exercise_feedback",
            columns: TrainingBlockFeedbackUniqueIndexColumns,
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "exercise_feedback",
            schema: "football");
    }
}
