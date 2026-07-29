using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260729233000_Phase14FactorySaveImports")]
public sealed class Phase14FactorySaveImports : Migration
{
    private static readonly string[] FactoryNameLookup =
        ["OrganizationId", "Name"];

    private static readonly string[] SnapshotImportLookup =
        ["OrganizationId", "ImportedAt"];

    private static readonly string[] SnapshotHashLookup =
        ["OrganizationId", "ContentSha256"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "factory_insights");

        migrationBuilder.CreateTable(
            name: "factory_sites",
            schema: "factory_insights",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Name = table.Column<string>(
                    type: "character varying(120)",
                    maxLength: 120,
                    nullable: false),
                Description = table.Column<string>(
                    type: "character varying(500)",
                    maxLength: 500,
                    nullable: true),
                CenterX = table.Column<double>(
                    type: "double precision",
                    nullable: true),
                CenterY = table.Column<double>(
                    type: "double precision",
                    nullable: true),
                RadiusMeters = table.Column<double>(
                    type: "double precision",
                    nullable: true),
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
                table.PrimaryKey("PK_factory_sites", factory => factory.Id);
                table.ForeignKey(
                    name: "FK_factory_sites_organizations_OrganizationId",
                    column: factory => factory.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "save_snapshots",
            schema: "factory_insights",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OrganizationId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                ImportedByMemberId = table.Column<Guid>(
                    type: "uuid",
                    nullable: false),
                Source = table.Column<string>(
                    type: "character varying(24)",
                    maxLength: 24,
                    nullable: false),
                OriginalFileName = table.Column<string>(
                    type: "character varying(180)",
                    maxLength: 180,
                    nullable: false),
                ContentSha256 = table.Column<string>(
                    type: "character varying(64)",
                    maxLength: 64,
                    nullable: false),
                FileSizeBytes = table.Column<long>(
                    type: "bigint",
                    nullable: false),
                SaveName = table.Column<string>(
                    type: "character varying(180)",
                    maxLength: 180,
                    nullable: true),
                SessionName = table.Column<string>(
                    type: "character varying(180)",
                    maxLength: 180,
                    nullable: true),
                MapName = table.Column<string>(
                    type: "character varying(180)",
                    maxLength: 180,
                    nullable: true),
                SaveVersion = table.Column<int>(
                    type: "integer",
                    nullable: true),
                BuildVersion = table.Column<int>(
                    type: "integer",
                    nullable: true),
                PlayDurationSeconds = table.Column<long>(
                    type: "bigint",
                    nullable: true),
                SavedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: true),
                IsModdedSave = table.Column<bool>(
                    type: "boolean",
                    nullable: true),
                ParserVersion = table.Column<string>(
                    type: "character varying(32)",
                    maxLength: 32,
                    nullable: false),
                AnalysisJson = table.Column<string>(
                    type: "jsonb",
                    nullable: false),
                ImportedAt = table.Column<DateTimeOffset>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_save_snapshots", snapshot => snapshot.Id);
                table.ForeignKey(
                    name: "FK_save_snapshots_organization_members_ImportedByMemberId",
                    column: snapshot => snapshot.ImportedByMemberId,
                    principalSchema: "members",
                    principalTable: "organization_members",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_save_snapshots_organizations_OrganizationId",
                    column: snapshot => snapshot.OrganizationId,
                    principalSchema: "organizations",
                    principalTable: "organizations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_factory_sites_OrganizationId_Name",
            schema: "factory_insights",
            table: "factory_sites",
            columns: FactoryNameLookup);
        migrationBuilder.CreateIndex(
            name: "IX_save_snapshots_ImportedByMemberId",
            schema: "factory_insights",
            table: "save_snapshots",
            column: "ImportedByMemberId");
        migrationBuilder.CreateIndex(
            name: "IX_save_snapshots_OrganizationId_ImportedAt",
            schema: "factory_insights",
            table: "save_snapshots",
            columns: SnapshotImportLookup);
        migrationBuilder.CreateIndex(
            name: "IX_save_snapshots_OrganizationId_ContentSha256",
            schema: "factory_insights",
            table: "save_snapshots",
            columns: SnapshotHashLookup,
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "factory_sites",
            schema: "factory_insights");
        migrationBuilder.DropTable(
            name: "save_snapshots",
            schema: "factory_insights");
    }
}
