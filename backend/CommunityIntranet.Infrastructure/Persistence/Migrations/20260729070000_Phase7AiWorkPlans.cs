using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729070000_Phase7AiWorkPlans")]
public partial class Phase7AiWorkPlans : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "ai");

        migrationBuilder.CreateTable(
            name: "work_plan_drafts",
            schema: "ai",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                CreatedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Prompt = table.Column<string>(
                    type: "character varying(2000)",
                    maxLength: 2000,
                    nullable: false),
                Tone = table.Column<string>(
                    type: "character varying(24)",
                    maxLength: 24,
                    nullable: false),
                ProposalJson = table.Column<string>(
                    type: "jsonb",
                    nullable: false),
                Model = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ConfirmedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                ProjectId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                ConcurrencyToken = table.Column<Guid>(
                    type: "uuid",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_work_plan_drafts", item => item.Id);
                table.ForeignKey(
                    name:
                        "FK_work_plan_drafts_organization_members_CreatedByMemberId",
                    column: item => item.CreatedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_work_plan_drafts_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_work_plan_drafts_projects_ProjectId",
                    column: item => item.ProjectId,
                    principalSchema: "projects",
                    principalTable: "projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_work_plan_drafts_CreatedByMemberId",
            schema: "ai",
            table: "work_plan_drafts",
            column: "CreatedByMemberId");
        migrationBuilder.CreateIndex(
            name:
                "IX_work_plan_drafts_OrganizationId_CreatedByMemberId_CreatedAt",
            schema: "ai",
            table: "work_plan_drafts",
            columns:
            [
                "OrganizationId",
                "CreatedByMemberId",
                "CreatedAt"
            ]);
        migrationBuilder.CreateIndex(
            name: "IX_work_plan_drafts_ProjectId",
            schema: "ai",
            table: "work_plan_drafts",
            column: "ProjectId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "work_plan_drafts",
            schema: "ai");
    }
}
