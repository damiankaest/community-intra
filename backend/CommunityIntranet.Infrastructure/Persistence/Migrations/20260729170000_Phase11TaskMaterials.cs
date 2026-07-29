using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729170000_Phase11TaskMaterials")]
public partial class Phase11TaskMaterials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "task_material_items",
            schema: "tasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(
                    type: "character varying(160)",
                    maxLength: 160,
                    nullable: false),
                Quantity = table.Column<string>(
                    type: "character varying(80)",
                    maxLength: 80,
                    nullable: false),
                Notes = table.Column<string>(
                    type: "character varying(300)",
                    maxLength: 300,
                    nullable: true),
                IsPrepared = table.Column<bool>(
                    type: "boolean",
                    nullable: false),
                PreparedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                PreparedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                SortOrder = table.Column<int>(
                    type: "integer",
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
                table.PrimaryKey("PK_task_material_items", item => item.Id);
                table.ForeignKey(
                    name: "FK_task_material_items_organization_members_PreparedByMemberId",
                    column: item => item.PreparedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_task_material_items_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_task_material_items_tasks_TaskId",
                    column: item => item.TaskId,
                    principalSchema: "tasks",
                    principalTable: "tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_task_material_items_PreparedByMemberId",
            schema: "tasks",
            table: "task_material_items",
            column: "PreparedByMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_task_material_items_TaskId",
            schema: "tasks",
            table: "task_material_items",
            column: "TaskId");
        migrationBuilder.CreateIndex(
            name: "IX_task_material_items_OrganizationId_TaskId",
            schema: "tasks",
            table: "task_material_items",
            columns: new[] { "OrganizationId", "TaskId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "task_material_items",
            schema: "tasks");
    }
}
