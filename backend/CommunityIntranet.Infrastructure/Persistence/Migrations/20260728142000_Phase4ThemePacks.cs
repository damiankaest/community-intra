using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260728142000_Phase4ThemePacks")]
public partial class Phase4ThemePacks : Migration
{
    private static readonly string[] ThemePackVersionColumns =
        ["Key", "Version"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "theme_packs");

        migrationBuilder.CreateTable(
            name: "theme_packs",
            schema: "theme_packs",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Key = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false),
                Name = table.Column<string>(
                    type: "character varying(120)",
                    maxLength: 120,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(1000)",
                    maxLength: 1000,
                    nullable: false),
                Version = table.Column<string>(
                    type: "character varying(32)",
                    maxLength: 32,
                    nullable: false),
                Author = table.Column<string>(
                    type: "character varying(120)",
                    maxLength: 120,
                    nullable: false),
                IsSystemTheme = table.Column<bool>(
                    type: "boolean",
                    nullable: false),
                ConfigurationJson = table.Column<string>(
                    type: "jsonb",
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
                table.PrimaryKey("PK_theme_packs", item => item.Id);
            });

        migrationBuilder.AddColumn<string[]>(
            name: "EnabledModules",
            schema: "organizations",
            table: "organizations",
            type: "text[]",
            nullable: false,
            defaultValue: Array.Empty<string>());

        migrationBuilder.CreateIndex(
            name: "IX_theme_packs_IsSystemTheme",
            schema: "theme_packs",
            table: "theme_packs",
            column: "IsSystemTheme");

        migrationBuilder.CreateIndex(
            name: "IX_theme_packs_Key_Version",
            schema: "theme_packs",
            table: "theme_packs",
            columns: ThemePackVersionColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_organizations_ThemePackId",
            schema: "organizations",
            table: "organizations",
            column: "ThemePackId");

        migrationBuilder.AddForeignKey(
            name: "FK_organizations_theme_packs_ThemePackId",
            schema: "organizations",
            table: "organizations",
            column: "ThemePackId",
            principalSchema: "theme_packs",
            principalTable: "theme_packs",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_organizations_theme_packs_ThemePackId",
            schema: "organizations",
            table: "organizations");

        migrationBuilder.DropIndex(
            name: "IX_organizations_ThemePackId",
            schema: "organizations",
            table: "organizations");

        migrationBuilder.DropColumn(
            name: "EnabledModules",
            schema: "organizations",
            table: "organizations");

        migrationBuilder.DropTable(
            name: "theme_packs",
            schema: "theme_packs");
    }
}
