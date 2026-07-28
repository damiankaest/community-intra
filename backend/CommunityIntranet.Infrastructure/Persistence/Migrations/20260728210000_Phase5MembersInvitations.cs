using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260728210000_Phase5MembersInvitations")]
public partial class Phase5MembersInvitations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "departments",
            schema: "members",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Name = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(500)",
                    maxLength: 500,
                    nullable: true),
                SortOrder = table.Column<int>(
                    type: "integer",
                    nullable: false),
                Icon = table.Column<string>(
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false),
                IsArchived = table.Column<bool>(
                    type: "boolean",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_departments", item => item.Id);
                table.ForeignKey(
                    name: "FK_departments_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "organization_invitations",
            schema: "members",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                TokenHash = table.Column<string>(
                    type: "character(64)",
                    fixedLength: true,
                    maxLength: 64,
                    nullable: false),
                CreatedByUserId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                DefaultPermissionRole = table.Column<string>(
                    type: "character varying(30)",
                    maxLength: 30,
                    nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false),
                MaximumUses = table.Column<int>(
                    type: "integer",
                    nullable: false),
                CurrentUses = table.Column<int>(
                    type: "integer",
                    nullable: false),
                IsRevoked = table.Column<bool>(
                    type: "boolean",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_organization_invitations",
                    item => item.Id);
                table.ForeignKey(
                    name: "FK_organization_invitations_users_CreatedByUserId",
                    column: item => item.CreatedByUserId,
                    principalSchema: "identity",
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_organization_invitations_organizations_OrganizationId",
                    column: item => item.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_organization_members_DepartmentId",
            schema: "members",
            table: "organization_members",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_departments_OrganizationId_IsArchived_SortOrder",
            schema: "members",
            table: "departments",
            columns: ["OrganizationId", "IsArchived", "SortOrder"]);

        migrationBuilder.CreateIndex(
            name: "IX_departments_OrganizationId_Name",
            schema: "members",
            table: "departments",
            columns: ["OrganizationId", "Name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_organization_invitations_CreatedByUserId",
            schema: "members",
            table: "organization_invitations",
            column: "CreatedByUserId");

        migrationBuilder.CreateIndex(
            name: "IX_organization_invitations_OrganizationId_IsRevoked_ExpiresAt",
            schema: "members",
            table: "organization_invitations",
            columns: ["OrganizationId", "IsRevoked", "ExpiresAt"]);

        migrationBuilder.CreateIndex(
            name: "IX_organization_invitations_TokenHash",
            schema: "members",
            table: "organization_invitations",
            column: "TokenHash",
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_organization_members_departments_DepartmentId",
            schema: "members",
            table: "organization_members",
            column: "DepartmentId",
            principalSchema: "members",
            principalTable: "departments",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_organization_members_departments_DepartmentId",
            schema: "members",
            table: "organization_members");

        migrationBuilder.DropIndex(
            name: "IX_organization_members_DepartmentId",
            schema: "members",
            table: "organization_members");

        migrationBuilder.DropTable(
            name: "organization_invitations",
            schema: "members");

        migrationBuilder.DropTable(
            name: "departments",
            schema: "members");
    }
}
