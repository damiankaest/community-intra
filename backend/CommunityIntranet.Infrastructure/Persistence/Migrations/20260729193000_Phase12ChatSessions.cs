using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729193000_Phase12ChatSessions")]
public sealed class Phase12ChatSessions : Migration
{
    private static readonly string[] ActiveConversationIndexColumns =
        ["OrganizationId", "MemberId", "ArchivedAt", "UpdatedAt"];

    private static readonly string[] LegacyConversationIndexColumns =
        ["OrganizationId", "MemberId", "UpdatedAt"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_conversations_OrganizationId_MemberId_UpdatedAt",
            schema: "ai",
            table: "conversations");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ArchivedAt",
            schema: "ai",
            table: "conversations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Title",
            schema: "ai",
            table: "conversations",
            type: "character varying(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.CreateIndex(
            name:
                "IX_conversations_OrganizationId_MemberId_ArchivedAt_UpdatedAt",
            schema: "ai",
            table: "conversations",
            columns: ActiveConversationIndexColumns);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name:
                "IX_conversations_OrganizationId_MemberId_ArchivedAt_UpdatedAt",
            schema: "ai",
            table: "conversations");

        migrationBuilder.DropColumn(
            name: "ArchivedAt",
            schema: "ai",
            table: "conversations");

        migrationBuilder.DropColumn(
            name: "Title",
            schema: "ai",
            table: "conversations");

        migrationBuilder.CreateIndex(
            name: "IX_conversations_OrganizationId_MemberId_UpdatedAt",
            schema: "ai",
            table: "conversations",
            columns: LegacyConversationIndexColumns);
    }
}
