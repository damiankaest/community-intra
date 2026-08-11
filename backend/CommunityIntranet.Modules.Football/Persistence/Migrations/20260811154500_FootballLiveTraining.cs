using CommunityIntranet.Modules.Football.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Modules.Football.Persistence.Migrations;

[DbContext(typeof(FootballDbContext))]
[Migration("20260811154500_FootballLiveTraining")]
public sealed class FootballLiveTraining : Migration
{
    private static readonly string[] RunUniqueIndexColumns = ["OrganizationId", "SessionId"];
    private static readonly string[] BlockRunUniqueIndexColumns = ["OrganizationId", "SessionId", "TrainingBlockId"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "live_training_runs",
            schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ActiveTrainingBlockId = table.Column<Guid>(type: "uuid", nullable: true),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PausedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AccumulatedPausedSeconds = table.Column<int>(type: "integer", nullable: false),
                UpdatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_live_training_runs", x => x.Id);
                table.ForeignKey(
                    name: "FK_live_training_runs_sessions_SessionId",
                    column: x => x.SessionId,
                    principalSchema: "football",
                    principalTable: "sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_live_training_runs_training_blocks_ActiveTrainingBlockId",
                    column: x => x.ActiveTrainingBlockId,
                    principalSchema: "football",
                    principalTable: "training_blocks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "live_training_block_runs",
            schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                TrainingBlockId = table.Column<Guid>(type: "uuid", nullable: false),
                StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                PausedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                AccumulatedSeconds = table.Column<int>(type: "integer", nullable: false),
                IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_live_training_block_runs", x => x.Id);
                table.ForeignKey(
                    name: "FK_live_training_block_runs_sessions_SessionId",
                    column: x => x.SessionId,
                    principalSchema: "football",
                    principalTable: "sessions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_live_training_block_runs_training_blocks_TrainingBlockId",
                    column: x => x.TrainingBlockId,
                    principalSchema: "football",
                    principalTable: "training_blocks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_live_training_runs_SessionId", schema: "football", table: "live_training_runs", column: "SessionId");
        migrationBuilder.CreateIndex(name: "IX_live_training_runs_ActiveTrainingBlockId", schema: "football", table: "live_training_runs", column: "ActiveTrainingBlockId");
        migrationBuilder.CreateIndex(name: "IX_live_training_runs_OrganizationId_SessionId", schema: "football", table: "live_training_runs", columns: RunUniqueIndexColumns, unique: true);
        migrationBuilder.CreateIndex(name: "IX_live_training_block_runs_SessionId", schema: "football", table: "live_training_block_runs", column: "SessionId");
        migrationBuilder.CreateIndex(name: "IX_live_training_block_runs_TrainingBlockId", schema: "football", table: "live_training_block_runs", column: "TrainingBlockId");
        migrationBuilder.CreateIndex(name: "IX_live_training_block_runs_OrganizationId_SessionId_TrainingBlockId", schema: "football", table: "live_training_block_runs", columns: BlockRunUniqueIndexColumns, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "live_training_block_runs", schema: "football");
        migrationBuilder.DropTable(name: "live_training_runs", schema: "football");
    }
}
