using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729093000_Phase8ConversationalWorkspace")]
public partial class Phase8ConversationalWorkspace : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ParentTaskId",
            schema: "tasks",
            table: "tasks",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "conversations",
            schema: "ai",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Tone = table.Column<string>(
                    type: "character varying(24)",
                    maxLength: 24,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_conversations", item => item.Id);
                table.ForeignKey(
                    name: "FK_conversations_organization_members_MemberId",
                    column: item => item.MemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_conversations_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "task_attachments",
            schema: "tasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                UploadedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                FileName = table.Column<string>(
                    type: "character varying(240)",
                    maxLength: 240,
                    nullable: false),
                MediaType = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                Size = table.Column<long>(type: "bigint", nullable: false),
                Content = table.Column<byte[]>(
                    type: "bytea",
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_task_attachments", item => item.Id);
                table.ForeignKey(
                    name:
                        "FK_task_attachments_organization_members_UploadedByMemberId",
                    column: item => item.UploadedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_task_attachments_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_task_attachments_tasks_TaskId",
                    column: item => item.TaskId,
                    principalSchema: "tasks",
                    principalTable: "tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "task_comments",
            schema: "tasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                AuthorMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Body = table.Column<string>(
                    type: "character varying(2000)",
                    maxLength: 2000,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_task_comments", item => item.Id);
                table.ForeignKey(
                    name: "FK_task_comments_organization_members_AuthorMemberId",
                    column: item => item.AuthorMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_task_comments_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_task_comments_tasks_TaskId",
                    column: item => item.TaskId,
                    principalSchema: "tasks",
                    principalTable: "tasks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "actions",
            schema: "ai",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                ConversationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                RequestedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Kind = table.Column<string>(
                    type: "character varying(40)",
                    maxLength: 40,
                    nullable: false),
                PayloadJson = table.Column<string>(
                    type: "jsonb",
                    nullable: false),
                Status = table.Column<string>(
                    type: "character varying(24)",
                    maxLength: 24,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                CompletedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                ResultEntityId = table.Column<Guid>(
                    type: "uuid",
                    nullable: true),
                ConcurrencyToken = table.Column<Guid>(
                    type: "uuid",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_actions", item => item.Id);
                table.ForeignKey(
                    name: "FK_actions_conversations_ConversationId",
                    column: item => item.ConversationId,
                    principalSchema: "ai",
                    principalTable: "conversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name:
                        "FK_actions_organization_members_RequestedByMemberId",
                    column: item => item.RequestedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_actions_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "messages",
            schema: "ai",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                ConversationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                Role = table.Column<string>(
                    type: "character varying(24)",
                    maxLength: 24,
                    nullable: false),
                Content = table.Column<string>(
                    type: "character varying(12000)",
                    maxLength: 12000,
                    nullable: false),
                Model = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_messages", item => item.Id);
                table.ForeignKey(
                    name: "FK_messages_conversations_ConversationId",
                    column: item => item.ConversationId,
                    principalSchema: "ai",
                    principalTable: "conversations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_messages_organization_members_MemberId",
                    column: item => item.MemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_messages_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_tasks_OrganizationId_ParentTaskId",
            schema: "tasks",
            table: "tasks",
            columns: ["OrganizationId", "ParentTaskId"]);
        migrationBuilder.CreateIndex(
            name: "IX_tasks_ParentTaskId",
            schema: "tasks",
            table: "tasks",
            column: "ParentTaskId");
        migrationBuilder.AddForeignKey(
            name: "FK_tasks_tasks_ParentTaskId",
            schema: "tasks",
            table: "tasks",
            column: "ParentTaskId",
            principalSchema: "tasks",
            principalTable: "tasks",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);

        migrationBuilder.CreateIndex(
            name:
                "IX_conversations_OrganizationId_MemberId_UpdatedAt",
            schema: "ai",
            table: "conversations",
            columns: ["OrganizationId", "MemberId", "UpdatedAt"]);
        migrationBuilder.CreateIndex(
            name: "IX_conversations_MemberId",
            schema: "ai",
            table: "conversations",
            column: "MemberId");
        migrationBuilder.CreateIndex(
            name: "IX_task_attachments_OrganizationId_TaskId_CreatedAt",
            schema: "tasks",
            table: "task_attachments",
            columns: ["OrganizationId", "TaskId", "CreatedAt"]);
        migrationBuilder.CreateIndex(
            name: "IX_task_attachments_TaskId",
            schema: "tasks",
            table: "task_attachments",
            column: "TaskId");
        migrationBuilder.CreateIndex(
            name: "IX_task_attachments_UploadedByMemberId",
            schema: "tasks",
            table: "task_attachments",
            column: "UploadedByMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_task_comments_AuthorMemberId",
            schema: "tasks",
            table: "task_comments",
            column: "AuthorMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_task_comments_OrganizationId_TaskId_CreatedAt",
            schema: "tasks",
            table: "task_comments",
            columns: ["OrganizationId", "TaskId", "CreatedAt"]);
        migrationBuilder.CreateIndex(
            name: "IX_task_comments_TaskId",
            schema: "tasks",
            table: "task_comments",
            column: "TaskId");
        migrationBuilder.CreateIndex(
            name: "IX_actions_ConversationId",
            schema: "ai",
            table: "actions",
            column: "ConversationId");
        migrationBuilder.CreateIndex(
            name:
                "IX_actions_OrganizationId_ConversationId_Status_CreatedAt",
            schema: "ai",
            table: "actions",
            columns:
            [
                "OrganizationId",
                "ConversationId",
                "Status",
                "CreatedAt"
            ]);
        migrationBuilder.CreateIndex(
            name: "IX_actions_RequestedByMemberId",
            schema: "ai",
            table: "actions",
            column: "RequestedByMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_messages_ConversationId",
            schema: "ai",
            table: "messages",
            column: "ConversationId");
        migrationBuilder.CreateIndex(
            name: "IX_messages_MemberId",
            schema: "ai",
            table: "messages",
            column: "MemberId");
        migrationBuilder.CreateIndex(
            name:
                "IX_messages_OrganizationId_ConversationId_CreatedAt",
            schema: "ai",
            table: "messages",
            columns: ["OrganizationId", "ConversationId", "CreatedAt"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "actions",
            schema: "ai");
        migrationBuilder.DropTable(
            name: "messages",
            schema: "ai");
        migrationBuilder.DropTable(
            name: "task_attachments",
            schema: "tasks");
        migrationBuilder.DropTable(
            name: "task_comments",
            schema: "tasks");
        migrationBuilder.DropTable(
            name: "conversations",
            schema: "ai");
        migrationBuilder.DropForeignKey(
            name: "FK_tasks_tasks_ParentTaskId",
            schema: "tasks",
            table: "tasks");
        migrationBuilder.DropIndex(
            name: "IX_tasks_OrganizationId_ParentTaskId",
            schema: "tasks",
            table: "tasks");
        migrationBuilder.DropIndex(
            name: "IX_tasks_ParentTaskId",
            schema: "tasks",
            table: "tasks");
        migrationBuilder.DropColumn(
            name: "ParentTaskId",
            schema: "tasks",
            table: "tasks");
    }
}
