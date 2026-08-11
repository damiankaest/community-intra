using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CommunityIntranet.Modules.Football.Persistence.Migrations;

[DbContext(typeof(FootballDbContext))]
[Migration("20260811094500_InitialFootball")]
public sealed class InitialFootball : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "football");

        migrationBuilder.CreateTable(
            name: "exercises", schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Location = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Intensity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                MinPlayers = table.Column<int>(type: "integer", nullable: false),
                MaxPlayers = table.Column<int>(type: "integer", nullable: true),
                DefaultDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                Focus = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                Equipment = table.Column<string[]>(type: "text[]", nullable: false),
                Tags = table.Column<string[]>(type: "text[]", nullable: false),
                CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsArchived = table.Column<bool>(type: "boolean", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_exercises", x => x.Id));

        migrationBuilder.CreateTable(
            name: "member_profiles", schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                TeamRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Position = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                ShirtNumber = table.Column<int>(type: "integer", nullable: true),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Strengths = table.Column<string[]>(type: "text[]", nullable: false),
                DevelopmentAreas = table.Column<string[]>(type: "text[]", nullable: false),
                SecondaryPositions = table.Column<string[]>(type: "text[]", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_member_profiles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "sessions", schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Focus = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Location = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                Opponent = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true),
                StartsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                CreatedByMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsCancelled = table.Column<bool>(type: "boolean", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_sessions", x => x.Id));

        migrationBuilder.CreateTable(
            name: "attendance", schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false), OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                SessionId = table.Column<Guid>(type: "uuid", nullable: false), MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false), Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            }, constraints: table => table.PrimaryKey("PK_attendance", x => x.Id));

        migrationBuilder.CreateTable(
            name: "training_blocks", schema: "football",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false), OrganizationId = table.Column<Guid>(type: "uuid", nullable: false), SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                ExerciseId = table.Column<Guid>(type: "uuid", nullable: true), Title = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true), CoachingPoints = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false), DurationMinutes = table.Column<int>(type: "integer", nullable: false), ResponsibleMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                AiReason = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true)
            }, constraints: table => table.PrimaryKey("PK_training_blocks", x => x.Id));

        migrationBuilder.CreateIndex("IX_exercises_OrganizationId_IsArchived_Category", "exercises", new[] { "OrganizationId", "IsArchived", "Category" }, schema: "football");
        migrationBuilder.CreateIndex("IX_member_profiles_OrganizationId_MemberId", "member_profiles", new[] { "OrganizationId", "MemberId" }, schema: "football", unique: true);
        migrationBuilder.CreateIndex("IX_sessions_OrganizationId_StartsAt", "sessions", new[] { "OrganizationId", "StartsAt" }, schema: "football");
        migrationBuilder.CreateIndex("IX_attendance_OrganizationId_SessionId_MemberId", "attendance", new[] { "OrganizationId", "SessionId", "MemberId" }, schema: "football", unique: true);
        migrationBuilder.CreateIndex("IX_training_blocks_OrganizationId_SessionId_SortOrder", "training_blocks", new[] { "OrganizationId", "SessionId", "SortOrder" }, schema: "football");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("attendance", "football");
        migrationBuilder.DropTable("training_blocks", "football");
        migrationBuilder.DropTable("exercises", "football");
        migrationBuilder.DropTable("member_profiles", "football");
        migrationBuilder.DropTable("sessions", "football");
    }
}
