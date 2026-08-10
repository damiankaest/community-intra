using CommunityIntranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommunityIntranet.Infrastructure.Persistence.Migrations;

[DbContext(typeof(CommunityIntranetDbContext))]
[Migration("20260810113000_CounterStrikeFoundation")]
public sealed class CounterStrikeFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE SCHEMA IF NOT EXISTS counter_strike;

            CREATE TABLE identity.steam_identities (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "SteamId64" character varying(20) NOT NULL,
                "DisplayName" character varying(100) NOT NULL,
                "AvatarUrl" character varying(500),
                "LinkedAt" timestamp with time zone NOT NULL,
                "ProfileUpdatedAt" timestamp with time zone
            );
            CREATE UNIQUE INDEX "IX_steam_identities_UserId" ON identity.steam_identities ("UserId");
            CREATE UNIQUE INDEX "IX_steam_identities_SteamId64" ON identity.steam_identities ("SteamId64");

            CREATE TABLE counter_strike.community_settings (
                "OrganizationId" uuid PRIMARY KEY,
                "ActiveSeasonId" uuid,
                "DemoMaximumMegabytes" integer NOT NULL,
                "IsEnabled" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );
            CREATE TABLE counter_strike.seasons (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL,
                "Name" character varying(120) NOT NULL, "StartsAt" timestamp with time zone NOT NULL,
                "EndsAt" timestamp with time zone, "IsActive" boolean NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_seasons_org_active" ON counter_strike.seasons ("OrganizationId") WHERE "IsActive";

            CREATE TABLE counter_strike.matches (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "SeasonId" uuid NOT NULL,
                "UploadedByUserId" uuid NOT NULL, "UploadedByMemberId" uuid NOT NULL,
                "DemoChecksum" character varying(64) NOT NULL, "OriginalFileName" character varying(255) NOT NULL,
                "DemoStoragePath" character varying(1000) NOT NULL, "AnalyzerArtifactPath" character varying(1000),
                "Status" integer NOT NULL, "FailureCode" character varying(80), "FailureMessage" character varying(500),
                "AttemptCount" integer NOT NULL, "MapName" character varying(80), "PlayedAt" timestamp with time zone,
                "TeamAName" character varying(120), "TeamBName" character varying(120),
                "TeamAScore" integer NOT NULL, "TeamBScore" integer NOT NULL, "CommunityTeam" character varying(1),
                "UploadedAt" timestamp with time zone NOT NULL, "ProcessingStartedAt" timestamp with time zone,
                "CompletedAt" timestamp with time zone
            );
            CREATE UNIQUE INDEX "IX_cs_matches_org_checksum" ON counter_strike.matches ("OrganizationId", "DemoChecksum");
            CREATE INDEX "IX_cs_matches_org_season_played" ON counter_strike.matches ("OrganizationId", "SeasonId", "PlayedAt");

            CREATE TABLE counter_strike.match_players (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "MatchId" uuid NOT NULL, "UserId" uuid,
                "SteamId64" character varying(20) NOT NULL, "DisplayName" character varying(100) NOT NULL,
                "TeamName" character varying(120) NOT NULL, "Kills" integer NOT NULL, "Deaths" integer NOT NULL,
                "Assists" integer NOT NULL, "Adr" double precision NOT NULL, "Kast" double precision NOT NULL,
                "HeadshotPercent" double precision NOT NULL, "UtilityDamage" integer NOT NULL,
                "FirstKills" integer NOT NULL, "FirstDeaths" integer NOT NULL, "TradeKills" integer NOT NULL,
                "BombPlants" integer NOT NULL, "BombDefuses" integer NOT NULL, "HltvRating" double precision NOT NULL,
                "ThreeKills" integer NOT NULL, "FourKills" integer NOT NULL, "Aces" integer NOT NULL,
                "ClutchesWon" integer NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_match_players_match_steam" ON counter_strike.match_players ("OrganizationId", "MatchId", "SteamId64");
            CREATE INDEX "IX_cs_match_players_org_user" ON counter_strike.match_players ("OrganizationId", "UserId");

            CREATE TABLE counter_strike.rounds (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "MatchId" uuid NOT NULL,
                "Number" integer NOT NULL, "StartTick" integer NOT NULL, "EndTick" integer NOT NULL,
                "WinnerTeam" character varying(120) NOT NULL, "TeamAScore" integer NOT NULL, "TeamBScore" integer NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_rounds_match_number" ON counter_strike.rounds ("MatchId", "Number");

            CREATE TABLE counter_strike.player_stats (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "SeasonId" uuid NOT NULL, "UserId" uuid NOT NULL,
                "Matches" integer NOT NULL, "Wins" integer NOT NULL, "Kills" integer NOT NULL, "Deaths" integer NOT NULL,
                "Assists" integer NOT NULL, "Adr" double precision NOT NULL, "Kast" double precision NOT NULL,
                "HeadshotPercent" double precision NOT NULL, "HltvRating" double precision NOT NULL,
                "UtilityDamage" integer NOT NULL, "FirstKills" integer NOT NULL, "FirstDeaths" integer NOT NULL,
                "TradeKills" integer NOT NULL, "ThreeKills" integer NOT NULL, "FourKills" integer NOT NULL,
                "Aces" integer NOT NULL, "ClutchesWon" integer NOT NULL, "Role" integer NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_player_stats_season_user" ON counter_strike.player_stats ("OrganizationId", "SeasonId", "UserId");

            CREATE TABLE counter_strike.highlights (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "SeasonId" uuid NOT NULL, "MatchId" uuid NOT NULL,
                "UserId" uuid, "SteamId64" character varying(20) NOT NULL, "PlayerName" character varying(100) NOT NULL,
                "RoundNumber" integer NOT NULL, "Type" character varying(60) NOT NULL, "Title" character varying(180) NOT NULL,
                "Score" integer NOT NULL, "StartTick" integer NOT NULL, "EndTick" integer,
                "VideoStoragePath" character varying(1000), "CreatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_highlights_rule" ON counter_strike.highlights ("MatchId", "RoundNumber", "Type", "SteamId64");
            CREATE INDEX "IX_cs_highlights_season_score" ON counter_strike.highlights ("OrganizationId", "SeasonId", "Score");

            CREATE TABLE counter_strike.highlight_reactions (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "HighlightId" uuid NOT NULL,
                "UserId" uuid NOT NULL, "Reaction" character varying(8) NOT NULL, "CreatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_reactions_user" ON counter_strike.highlight_reactions ("HighlightId", "UserId", "Reaction");

            CREATE TABLE counter_strike.awards (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "SeasonId" uuid NOT NULL,
                "Key" character varying(80) NOT NULL, "Name" character varying(120) NOT NULL,
                "Description" character varying(500) NOT NULL, "Icon" character varying(32) NOT NULL,
                "CreatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_awards_season_key" ON counter_strike.awards ("OrganizationId", "SeasonId", "Key");
            CREATE TABLE counter_strike.award_assignments (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "AwardId" uuid NOT NULL,
                "UserId" uuid NOT NULL, "Value" double precision NOT NULL, "AssignedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_award_assignments_user" ON counter_strike.award_assignments ("AwardId", "UserId");

            CREATE TABLE counter_strike.game_sessions (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "CreatedByUserId" uuid NOT NULL,
                "SessionDate" timestamp with time zone NOT NULL, "PlannedStart" time without time zone,
                "CreatedAt" timestamp with time zone NOT NULL, "IsClosed" boolean NOT NULL
            );
            CREATE INDEX "IX_cs_game_sessions_date" ON counter_strike.game_sessions ("OrganizationId", "SessionDate");
            CREATE TABLE counter_strike.game_session_participants (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "GameSessionId" uuid NOT NULL,
                "UserId" uuid NOT NULL, "Availability" integer NOT NULL, "AvailableFrom" time without time zone,
                "UpdatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_game_participants_user" ON counter_strike.game_session_participants ("GameSessionId", "UserId");

            CREATE TABLE counter_strike.training_plans (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "UserId" uuid NOT NULL,
                "PlanDate" date NOT NULL, "PlannedMinutes" integer NOT NULL,
                "RecommendationReason" character varying(500), "CreatedAt" timestamp with time zone NOT NULL,
                "CompletedAt" timestamp with time zone
            );
            CREATE INDEX "IX_cs_training_plans_user_date" ON counter_strike.training_plans ("OrganizationId", "UserId", "PlanDate");
            CREATE TABLE counter_strike.training_exercises (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "TrainingPlanId" uuid, "Kind" integer NOT NULL,
                "Name" character varying(160) NOT NULL, "Description" character varying(1000) NOT NULL,
                "DurationMinutes" integer NOT NULL, "MapName" character varying(80), "Position" character varying(300),
                "Target" character varying(300), "MediaUrl" character varying(1000), "SortOrder" integer NOT NULL
            );
            CREATE INDEX "IX_cs_training_exercises_plan" ON counter_strike.training_exercises ("OrganizationId", "TrainingPlanId", "SortOrder");
            CREATE TABLE counter_strike.training_sessions (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "UserId" uuid NOT NULL,
                "TrainingPlanId" uuid, "StartedAt" timestamp with time zone NOT NULL,
                "CompletedAt" timestamp with time zone, "DurationSeconds" integer NOT NULL
            );
            CREATE INDEX "IX_cs_training_sessions_user" ON counter_strike.training_sessions ("OrganizationId", "UserId", "StartedAt");
            CREATE TABLE counter_strike.training_results (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "UserId" uuid NOT NULL,
                "TrainingSessionId" uuid, "TrainingExerciseId" uuid, "Kind" integer NOT NULL,
                "Hits" integer NOT NULL, "Misses" integer NOT NULL, "Accuracy" double precision NOT NULL,
                "ReactionTimeMs" double precision NOT NULL, "FlickTimeMs" double precision NOT NULL,
                "TrackingPercent" double precision NOT NULL, "Repetitions" integer NOT NULL,
                "CompletedAt" timestamp with time zone NOT NULL
            );
            CREATE INDEX "IX_cs_training_results_user" ON counter_strike.training_results ("OrganizationId", "UserId", "CompletedAt");

            CREATE TABLE counter_strike.weekly_challenges (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "SeasonId" uuid NOT NULL,
                "Name" character varying(160) NOT NULL, "Description" character varying(600) NOT NULL,
                "MetricKey" character varying(80) NOT NULL, "TargetValue" double precision NOT NULL,
                "StartsAt" timestamp with time zone NOT NULL, "EndsAt" timestamp with time zone NOT NULL
            );
            CREATE INDEX "IX_cs_weekly_challenges_date" ON counter_strike.weekly_challenges ("OrganizationId", "StartsAt", "EndsAt");
            CREATE TABLE counter_strike.weekly_challenge_progress (
                "Id" uuid PRIMARY KEY, "OrganizationId" uuid NOT NULL, "ChallengeId" uuid NOT NULL,
                "UserId" uuid NOT NULL, "Value" double precision NOT NULL,
                "CompletedAt" timestamp with time zone, "UpdatedAt" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX "IX_cs_challenge_progress_user" ON counter_strike.weekly_challenge_progress ("ChallengeId", "UserId");

            CREATE INDEX "IX_matches_SeasonId" ON counter_strike.matches ("SeasonId");
            CREATE INDEX "IX_match_players_MatchId" ON counter_strike.match_players ("MatchId");
            CREATE INDEX "IX_player_stats_SeasonId" ON counter_strike.player_stats ("SeasonId");
            CREATE INDEX "IX_training_exercises_TrainingPlanId" ON counter_strike.training_exercises ("TrainingPlanId");
            CREATE INDEX "IX_training_results_TrainingSessionId" ON counter_strike.training_results ("TrainingSessionId");

            ALTER TABLE identity.steam_identities ADD CONSTRAINT "FK_steam_identities_users_UserId"
                FOREIGN KEY ("UserId") REFERENCES identity.users ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.matches ADD CONSTRAINT "FK_matches_seasons_SeasonId"
                FOREIGN KEY ("SeasonId") REFERENCES counter_strike.seasons ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.match_players ADD CONSTRAINT "FK_match_players_matches_MatchId"
                FOREIGN KEY ("MatchId") REFERENCES counter_strike.matches ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.rounds ADD CONSTRAINT "FK_rounds_matches_MatchId"
                FOREIGN KEY ("MatchId") REFERENCES counter_strike.matches ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.player_stats ADD CONSTRAINT "FK_player_stats_seasons_SeasonId"
                FOREIGN KEY ("SeasonId") REFERENCES counter_strike.seasons ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.highlights ADD CONSTRAINT "FK_highlights_matches_MatchId"
                FOREIGN KEY ("MatchId") REFERENCES counter_strike.matches ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.highlight_reactions ADD CONSTRAINT "FK_highlight_reactions_highlights_HighlightId"
                FOREIGN KEY ("HighlightId") REFERENCES counter_strike.highlights ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.award_assignments ADD CONSTRAINT "FK_award_assignments_awards_AwardId"
                FOREIGN KEY ("AwardId") REFERENCES counter_strike.awards ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.game_session_participants ADD CONSTRAINT "FK_game_session_participants_game_sessions_GameSessionId"
                FOREIGN KEY ("GameSessionId") REFERENCES counter_strike.game_sessions ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.training_exercises ADD CONSTRAINT "FK_training_exercises_training_plans_TrainingPlanId"
                FOREIGN KEY ("TrainingPlanId") REFERENCES counter_strike.training_plans ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.training_results ADD CONSTRAINT "FK_training_results_training_sessions_TrainingSessionId"
                FOREIGN KEY ("TrainingSessionId") REFERENCES counter_strike.training_sessions ("Id") ON DELETE CASCADE;
            ALTER TABLE counter_strike.weekly_challenge_progress ADD CONSTRAINT "FK_weekly_challenge_progress_weekly_challenges_ChallengeId"
                FOREIGN KEY ("ChallengeId") REFERENCES counter_strike.weekly_challenges ("Id") ON DELETE CASCADE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP SCHEMA IF EXISTS counter_strike CASCADE;
            DROP TABLE IF EXISTS identity.steam_identities;
            """);
    }
}
