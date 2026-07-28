using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260728222000_Phase6CoreModules")]
public partial class Phase6CoreModules : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "projects");
        migrationBuilder.EnsureSchema(name: "tasks");
        migrationBuilder.EnsureSchema(name: "incidents");
        migrationBuilder.EnsureSchema(name: "awards");
        migrationBuilder.EnsureSchema(name: "activity");

        migrationBuilder.CreateTable(
            name: "projects",
            schema: "projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Name = table.Column<string>(
                    type: "character varying(160)",
                    maxLength: 160,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(4000)",
                    maxLength: 4000,
                    nullable: true),
                Status = table.Column<string>(
                    type: "character varying(30)",
                    maxLength: 30,
                    nullable: false),
                Priority = table.Column<string>(
                    type: "character varying(20)",
                    maxLength: 20,
                    nullable: false),
                OwnerMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                StartDate = table.Column<DateOnly>(
                    type: "date",
                    nullable: true),
                DueDate = table.Column<DateOnly>(
                    type: "date",
                    nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                ConcurrencyToken = table.Column<Guid>(
                    type: "uuid",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_projects", item => item.Id);
                table.ForeignKey(
                    name: "FK_projects_organization_members_OwnerMemberId",
                    column: item => item.OwnerMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_projects_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "incidents",
            schema: "incidents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Title = table.Column<string>(
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(6000)",
                    maxLength: 6000,
                    nullable: false),
                Category = table.Column<string>(
                    type: "character varying(120)",
                    maxLength: 120,
                    nullable: false),
                Severity = table.Column<string>(
                    type: "character varying(30)",
                    maxLength: 30,
                    nullable: false),
                Status = table.Column<string>(
                    type: "character varying(30)",
                    maxLength: 30,
                    nullable: false),
                ReportedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                ResponsibleMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                Resolution = table.Column<string>(
                    type: "character varying(6000)",
                    maxLength: 6000,
                    nullable: true),
                LessonsLearned = table.Column<string>(
                    type: "character varying(4000)",
                    maxLength: 4000,
                    nullable: true),
                OccurredAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ResolvedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                ConcurrencyToken = table.Column<Guid>(
                    type: "uuid",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_incidents", item => item.Id);
                table.ForeignKey(
                    name: "FK_incidents_organization_members_ReportedByMemberId",
                    column: item => item.ReportedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_incidents_organization_members_ResponsibleMemberId",
                    column: item => item.ResponsibleMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_incidents_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "awards",
            schema: "awards",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Name = table.Column<string>(
                    type: "character varying(160)",
                    maxLength: 160,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(2000)",
                    maxLength: 2000,
                    nullable: false),
                AwardedToMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                AwardedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                AwardedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                Icon = table.Column<string>(
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false),
                Category = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                IsPublic = table.Column<bool>(
                    type: "boolean",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_awards", item => item.Id);
                table.ForeignKey(
                    name: "FK_awards_organization_members_AwardedByMemberId",
                    column: item => item.AwardedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_awards_organization_members_AwardedToMemberId",
                    column: item => item.AwardedToMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_awards_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "activities",
            schema: "activity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                ActivityType = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                ActorMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                EntityType = table.Column<string>(
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false),
                EntityId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                DataJson = table.Column<string>(
                    type: "jsonb",
                    nullable: false),
                EventVersion = table.Column<int>(
                    type: "integer",
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_activities", item => item.Id);
                table.ForeignKey(
                    name: "FK_activities_organization_members_ActorMemberId",
                    column: item => item.ActorMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_activities_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "tasks",
            schema: "tasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                ProjectId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                Title = table.Column<string>(
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(4000)",
                    maxLength: 4000,
                    nullable: true),
                Status = table.Column<string>(
                    type: "character varying(30)",
                    maxLength: 30,
                    nullable: false),
                Priority = table.Column<string>(
                    type: "character varying(20)",
                    maxLength: 20,
                    nullable: false),
                AssignedMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                CreatedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                DueDate = table.Column<DateOnly>(
                    type: "date",
                    nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                ConcurrencyToken = table.Column<Guid>(
                    type: "uuid",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tasks", item => item.Id);
                table.ForeignKey(
                    name: "FK_tasks_organization_members_AssignedMemberId",
                    column: item => item.AssignedMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_tasks_organization_members_CreatedByMemberId",
                    column: item => item.CreatedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_tasks_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_tasks_projects_ProjectId",
                    column: item => item.ProjectId,
                    principalSchema: "projects",
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        CreateIndexes(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "activities", schema: "activity");
        migrationBuilder.DropTable(name: "awards", schema: "awards");
        migrationBuilder.DropTable(name: "incidents", schema: "incidents");
        migrationBuilder.DropTable(name: "tasks", schema: "tasks");
        migrationBuilder.DropTable(name: "projects", schema: "projects");
    }

    private static void CreateIndexes(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_projects_OrganizationId_Status",
            schema: "projects",
            table: "projects",
            columns: ["OrganizationId", "Status"]);
        migrationBuilder.CreateIndex(
            name: "IX_projects_OrganizationId_OwnerMemberId",
            schema: "projects",
            table: "projects",
            columns: ["OrganizationId", "OwnerMemberId"]);
        migrationBuilder.CreateIndex(
            name: "IX_projects_OwnerMemberId",
            schema: "projects",
            table: "projects",
            column: "OwnerMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_tasks_OrganizationId_Status",
            schema: "tasks",
            table: "tasks",
            columns: ["OrganizationId", "Status"]);
        migrationBuilder.CreateIndex(
            name: "IX_tasks_OrganizationId_AssignedMemberId",
            schema: "tasks",
            table: "tasks",
            columns: ["OrganizationId", "AssignedMemberId"]);
        migrationBuilder.CreateIndex(
            name: "IX_tasks_OrganizationId_ProjectId",
            schema: "tasks",
            table: "tasks",
            columns: ["OrganizationId", "ProjectId"]);
        migrationBuilder.CreateIndex(
            name: "IX_tasks_AssignedMemberId",
            schema: "tasks",
            table: "tasks",
            column: "AssignedMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_tasks_CreatedByMemberId",
            schema: "tasks",
            table: "tasks",
            column: "CreatedByMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_tasks_ProjectId",
            schema: "tasks",
            table: "tasks",
            column: "ProjectId");
        migrationBuilder.CreateIndex(
            name: "IX_incidents_OrganizationId_Status",
            schema: "incidents",
            table: "incidents",
            columns: ["OrganizationId", "Status"]);
        migrationBuilder.CreateIndex(
            name: "IX_incidents_OrganizationId_Severity",
            schema: "incidents",
            table: "incidents",
            columns: ["OrganizationId", "Severity"]);
        migrationBuilder.CreateIndex(
            name: "IX_incidents_ReportedByMemberId",
            schema: "incidents",
            table: "incidents",
            column: "ReportedByMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_incidents_ResponsibleMemberId",
            schema: "incidents",
            table: "incidents",
            column: "ResponsibleMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_awards_OrganizationId_AwardedAt",
            schema: "awards",
            table: "awards",
            columns: ["OrganizationId", "AwardedAt"]);
        migrationBuilder.CreateIndex(
            name: "IX_awards_OrganizationId_AwardedToMemberId",
            schema: "awards",
            table: "awards",
            columns: ["OrganizationId", "AwardedToMemberId"]);
        migrationBuilder.CreateIndex(
            name: "IX_awards_AwardedByMemberId",
            schema: "awards",
            table: "awards",
            column: "AwardedByMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_awards_AwardedToMemberId",
            schema: "awards",
            table: "awards",
            column: "AwardedToMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_activities_OrganizationId_CreatedAt",
            schema: "activity",
            table: "activities",
            columns: ["OrganizationId", "CreatedAt"]);
        migrationBuilder.CreateIndex(
            name: "IX_activities_OrganizationId_EntityType_EntityId",
            schema: "activity",
            table: "activities",
            columns: ["OrganizationId", "EntityType", "EntityId"]);
        migrationBuilder.CreateIndex(
            name: "IX_activities_ActorMemberId",
            schema: "activity",
            table: "activities",
            column: "ActorMemberId");
    }
}
