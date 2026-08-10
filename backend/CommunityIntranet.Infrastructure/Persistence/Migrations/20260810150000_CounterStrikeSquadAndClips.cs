using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260810150000_CounterStrikeSquadAndClips")]
public sealed class CounterStrikeSquadAndClips : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE counter_strike.community_settings
                ADD COLUMN "SquadName" character varying(120),
                ADD COLUMN "SquadTag" character varying(12);

            CREATE TABLE counter_strike.roster_members (
                "OrganizationId" uuid NOT NULL,
                "UserId" uuid NOT NULL,
                "Status" integer NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_roster_members" PRIMARY KEY ("OrganizationId", "UserId")
            );

            CREATE TABLE counter_strike.clips (
                "Id" uuid PRIMARY KEY,
                "OrganizationId" uuid NOT NULL,
                "UploadedByUserId" uuid NOT NULL,
                "UploadedByMemberId" uuid NOT NULL,
                "Title" character varying(120) NOT NULL,
                "Description" character varying(500),
                "OriginalFileName" character varying(255) NOT NULL,
                "StoragePath" character varying(1000) NOT NULL,
                "MimeType" character varying(80) NOT NULL,
                "SizeBytes" bigint NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL
            );
            CREATE INDEX "IX_cs_clips_org_created" ON counter_strike.clips ("OrganizationId", "CreatedAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS counter_strike.clips;
            DROP TABLE IF EXISTS counter_strike.roster_members;
            ALTER TABLE counter_strike.community_settings
                DROP COLUMN IF EXISTS "SquadName",
                DROP COLUMN IF EXISTS "SquadTag";
            """);
    }
}
