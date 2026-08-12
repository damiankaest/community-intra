using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260812090000_MysteryMvp")]
public sealed class MysteryMvp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE SCHEMA IF NOT EXISTS mystery;

            CREATE TABLE mystery.sessions (
                "Id" uuid NOT NULL,
                "JoinCode" character varying(8) NOT NULL,
                "Title" character varying(180) NOT NULL,
                "Status" character varying(24) NOT NULL,
                "GameMaster" character varying(80) NOT NULL,
                "Notice" character varying(500),
                "ConfigurationJson" jsonb NOT NULL,
                "SecretCaseJson" jsonb NOT NULL,
                "GameStateJson" jsonb NOT NULL,
                "Version" uuid NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_sessions" PRIMARY KEY ("Id")
            );

            CREATE UNIQUE INDEX "IX_sessions_JoinCode"
                ON mystery.sessions ("JoinCode");
            CREATE INDEX "IX_sessions_Status_UpdatedAt"
                ON mystery.sessions ("Status", "UpdatedAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TABLE IF EXISTS mystery.sessions;");
    }
}
