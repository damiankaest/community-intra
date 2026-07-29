using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729110000_Phase9CollaborationLoop")]
public partial class Phase9CollaborationLoop : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "notifications");

        migrationBuilder.AddColumn<byte[]>(
            name: "ThumbnailContent",
            schema: "tasks",
            table: "task_attachments",
            type: "bytea",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ThumbnailMediaType",
            schema: "tasks",
            table: "task_attachments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "notifications",
            schema: "notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                RecipientMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                ActorMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                NotificationType = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false),
                Title = table.Column<string>(
                    type: "character varying(200)",
                    maxLength: 200,
                    nullable: false),
                Body = table.Column<string>(
                    type: "character varying(500)",
                    maxLength: 500,
                    nullable: false),
                EntityType = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false),
                EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ReadAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notifications", item => item.Id);
                table.ForeignKey(
                    name: "FK_notifications_organization_members_ActorMemberId",
                    column: item => item.ActorMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name:
                        "FK_notifications_organization_members_RecipientMemberId",
                    column: item => item.RecipientMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_notifications_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_notifications_ActorMemberId",
            schema: "notifications",
            table: "notifications",
            column: "ActorMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_notifications_RecipientMemberId",
            schema: "notifications",
            table: "notifications",
            column: "RecipientMemberId");
        migrationBuilder.CreateIndex(
            name:
                "IX_notifications_OrganizationId_RecipientMemberId_ReadAt_CreatedAt",
            schema: "notifications",
            table: "notifications",
            columns:
            [
                "OrganizationId",
                "RecipientMemberId",
                "ReadAt",
                "CreatedAt"
            ]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notifications",
            schema: "notifications");
        migrationBuilder.DropColumn(
            name: "ThumbnailContent",
            schema: "tasks",
            table: "task_attachments");
        migrationBuilder.DropColumn(
            name: "ThumbnailMediaType",
            schema: "tasks",
            table: "task_attachments");
    }
}
