using CommunityIntranet.Modules.CounterStrike.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.CounterStrike.Persistence;

internal static class CounterStrikeConfiguration
{
    internal const string Schema = "counter_strike";
}

public sealed class CounterStrikeCommunitySettingsConfiguration
    : IEntityTypeConfiguration<CounterStrikeCommunitySettings>
{
    public void Configure(EntityTypeBuilder<CounterStrikeCommunitySettings> builder)
    {
        builder.ToTable("community_settings", CounterStrikeConfiguration.Schema);
        builder.HasKey(settings => settings.OrganizationId);
    }
}

public sealed class CounterStrikeSeasonConfiguration : IEntityTypeConfiguration<CounterStrikeSeason>
{
    public void Configure(EntityTypeBuilder<CounterStrikeSeason> builder)
    {
        builder.ToTable("seasons", CounterStrikeConfiguration.Schema);
        builder.HasKey(season => season.Id);
        builder.HasIndex(season => season.OrganizationId)
            .IsUnique()
            .HasFilter("\"IsActive\"")
            .HasDatabaseName("IX_cs_seasons_org_active");
        builder.Property(season => season.Name).HasMaxLength(120).IsRequired();
    }
}

public sealed class CounterStrikeMatchConfiguration : IEntityTypeConfiguration<CounterStrikeMatch>
{
    public void Configure(EntityTypeBuilder<CounterStrikeMatch> builder)
    {
        builder.ToTable("matches", CounterStrikeConfiguration.Schema);
        builder.HasKey(match => match.Id);
        builder.HasIndex(match => new { match.OrganizationId, match.DemoChecksum })
            .IsUnique().HasDatabaseName("IX_cs_matches_org_checksum");
        builder.HasIndex(match => new { match.OrganizationId, match.SeasonId, match.PlayedAt })
            .HasDatabaseName("IX_cs_matches_org_season_played");
        builder.Property(match => match.DemoChecksum).HasMaxLength(64).IsRequired();
        builder.Property(match => match.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(match => match.DemoStoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(match => match.AnalyzerArtifactPath).HasMaxLength(1000);
        builder.Property(match => match.FailureCode).HasMaxLength(80);
        builder.Property(match => match.FailureMessage).HasMaxLength(500);
        builder.Property(match => match.MapName).HasMaxLength(80);
        builder.Property(match => match.TeamAName).HasMaxLength(120);
        builder.Property(match => match.TeamBName).HasMaxLength(120);
        builder.Property(match => match.CommunityTeam).HasMaxLength(1);
        builder.HasOne<CounterStrikeSeason>().WithMany()
            .HasForeignKey(match => match.SeasonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeMatchPlayerConfiguration : IEntityTypeConfiguration<CounterStrikeMatchPlayer>
{
    public void Configure(EntityTypeBuilder<CounterStrikeMatchPlayer> builder)
    {
        builder.ToTable("match_players", CounterStrikeConfiguration.Schema);
        builder.HasKey(player => player.Id);
        builder.HasIndex(player => new { player.OrganizationId, player.MatchId, player.SteamId64 })
            .IsUnique().HasDatabaseName("IX_cs_match_players_match_steam");
        builder.HasIndex(player => new { player.OrganizationId, player.UserId })
            .HasDatabaseName("IX_cs_match_players_org_user");
        builder.Property(player => player.SteamId64).HasMaxLength(20).IsRequired();
        builder.Property(player => player.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(player => player.TeamName).HasMaxLength(120).IsRequired();
        builder.HasOne<CounterStrikeMatch>().WithMany()
            .HasForeignKey(player => player.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeRoundConfiguration : IEntityTypeConfiguration<CounterStrikeRound>
{
    public void Configure(EntityTypeBuilder<CounterStrikeRound> builder)
    {
        builder.ToTable("rounds", CounterStrikeConfiguration.Schema);
        builder.HasKey(round => round.Id);
        builder.HasIndex(round => new { round.MatchId, round.Number })
            .IsUnique().HasDatabaseName("IX_cs_rounds_match_number");
        builder.Property(round => round.WinnerTeam).HasMaxLength(120).IsRequired();
        builder.HasOne<CounterStrikeMatch>().WithMany()
            .HasForeignKey(round => round.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikePlayerStatsConfiguration : IEntityTypeConfiguration<CounterStrikePlayerStats>
{
    public void Configure(EntityTypeBuilder<CounterStrikePlayerStats> builder)
    {
        builder.ToTable("player_stats", CounterStrikeConfiguration.Schema);
        builder.HasKey(stats => stats.Id);
        builder.HasIndex(stats => new { stats.OrganizationId, stats.SeasonId, stats.UserId })
            .IsUnique().HasDatabaseName("IX_cs_player_stats_season_user");
        builder.HasOne<CounterStrikeSeason>().WithMany()
            .HasForeignKey(stats => stats.SeasonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeHighlightConfiguration : IEntityTypeConfiguration<CounterStrikeHighlight>
{
    public void Configure(EntityTypeBuilder<CounterStrikeHighlight> builder)
    {
        builder.ToTable("highlights", CounterStrikeConfiguration.Schema);
        builder.HasKey(highlight => highlight.Id);
        builder.HasIndex(highlight => new { highlight.OrganizationId, highlight.SeasonId, highlight.Score })
            .HasDatabaseName("IX_cs_highlights_season_score");
        builder.HasIndex(highlight => new { highlight.MatchId, highlight.RoundNumber, highlight.Type, highlight.SteamId64 })
            .IsUnique().HasDatabaseName("IX_cs_highlights_rule");
        builder.Property(highlight => highlight.SteamId64).HasMaxLength(20).IsRequired();
        builder.Property(highlight => highlight.PlayerName).HasMaxLength(100).IsRequired();
        builder.Property(highlight => highlight.Type).HasMaxLength(60).IsRequired();
        builder.Property(highlight => highlight.Title).HasMaxLength(180).IsRequired();
        builder.Property(highlight => highlight.VideoStoragePath).HasMaxLength(1000);
        builder.HasOne<CounterStrikeMatch>().WithMany()
            .HasForeignKey(highlight => highlight.MatchId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeHighlightReactionConfiguration : IEntityTypeConfiguration<CounterStrikeHighlightReaction>
{
    public void Configure(EntityTypeBuilder<CounterStrikeHighlightReaction> builder)
    {
        builder.ToTable("highlight_reactions", CounterStrikeConfiguration.Schema);
        builder.HasKey(reaction => reaction.Id);
        builder.HasIndex(reaction => new { reaction.HighlightId, reaction.UserId, reaction.Reaction })
            .IsUnique().HasDatabaseName("IX_cs_reactions_user");
        builder.Property(reaction => reaction.Reaction).HasMaxLength(8).IsRequired();
        builder.HasOne<CounterStrikeHighlight>().WithMany()
            .HasForeignKey(reaction => reaction.HighlightId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeAwardConfiguration : IEntityTypeConfiguration<CounterStrikeAward>
{
    public void Configure(EntityTypeBuilder<CounterStrikeAward> builder)
    {
        builder.ToTable("awards", CounterStrikeConfiguration.Schema);
        builder.HasKey(award => award.Id);
        builder.HasIndex(award => new { award.OrganizationId, award.SeasonId, award.Key })
            .IsUnique().HasDatabaseName("IX_cs_awards_season_key");
        builder.Property(award => award.Key).HasMaxLength(80).IsRequired();
        builder.Property(award => award.Name).HasMaxLength(120).IsRequired();
        builder.Property(award => award.Description).HasMaxLength(500).IsRequired();
        builder.Property(award => award.Icon).HasMaxLength(32).IsRequired();
    }
}

public sealed class CounterStrikeAwardAssignmentConfiguration : IEntityTypeConfiguration<CounterStrikeAwardAssignment>
{
    public void Configure(EntityTypeBuilder<CounterStrikeAwardAssignment> builder)
    {
        builder.ToTable("award_assignments", CounterStrikeConfiguration.Schema);
        builder.HasKey(assignment => assignment.Id);
        builder.HasIndex(assignment => new { assignment.AwardId, assignment.UserId })
            .IsUnique().HasDatabaseName("IX_cs_award_assignments_user");
        builder.HasOne<CounterStrikeAward>().WithMany()
            .HasForeignKey(assignment => assignment.AwardId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeGameSessionConfiguration : IEntityTypeConfiguration<CounterStrikeGameSession>
{
    public void Configure(EntityTypeBuilder<CounterStrikeGameSession> builder)
    {
        builder.ToTable("game_sessions", CounterStrikeConfiguration.Schema);
        builder.HasKey(session => session.Id);
        builder.HasIndex(session => new { session.OrganizationId, session.SessionDate })
            .HasDatabaseName("IX_cs_game_sessions_date");
    }
}

public sealed class CounterStrikeGameSessionParticipantConfiguration
    : IEntityTypeConfiguration<CounterStrikeGameSessionParticipant>
{
    public void Configure(EntityTypeBuilder<CounterStrikeGameSessionParticipant> builder)
    {
        builder.ToTable("game_session_participants", CounterStrikeConfiguration.Schema);
        builder.HasKey(participant => participant.Id);
        builder.HasIndex(participant => new { participant.GameSessionId, participant.UserId })
            .IsUnique().HasDatabaseName("IX_cs_game_participants_user");
        builder.HasOne<CounterStrikeGameSession>().WithMany()
            .HasForeignKey(participant => participant.GameSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeTrainingPlanConfiguration : IEntityTypeConfiguration<CounterStrikeTrainingPlan>
{
    public void Configure(EntityTypeBuilder<CounterStrikeTrainingPlan> builder)
    {
        builder.ToTable("training_plans", CounterStrikeConfiguration.Schema);
        builder.HasKey(plan => plan.Id);
        builder.HasIndex(plan => new { plan.OrganizationId, plan.UserId, plan.PlanDate })
            .HasDatabaseName("IX_cs_training_plans_user_date");
        builder.Property(plan => plan.RecommendationReason).HasMaxLength(500);
    }
}

public sealed class CounterStrikeTrainingExerciseConfiguration : IEntityTypeConfiguration<CounterStrikeTrainingExercise>
{
    public void Configure(EntityTypeBuilder<CounterStrikeTrainingExercise> builder)
    {
        builder.ToTable("training_exercises", CounterStrikeConfiguration.Schema);
        builder.HasKey(exercise => exercise.Id);
        builder.HasIndex(exercise => new { exercise.OrganizationId, exercise.TrainingPlanId, exercise.SortOrder })
            .HasDatabaseName("IX_cs_training_exercises_plan");
        builder.Property(exercise => exercise.Name).HasMaxLength(160).IsRequired();
        builder.Property(exercise => exercise.Description).HasMaxLength(1000).IsRequired();
        builder.Property(exercise => exercise.MapName).HasMaxLength(80);
        builder.Property(exercise => exercise.Position).HasMaxLength(300);
        builder.Property(exercise => exercise.Target).HasMaxLength(300);
        builder.Property(exercise => exercise.MediaUrl).HasMaxLength(1000);
        builder.HasOne<CounterStrikeTrainingPlan>().WithMany()
            .HasForeignKey(exercise => exercise.TrainingPlanId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeTrainingSessionConfiguration : IEntityTypeConfiguration<CounterStrikeTrainingSession>
{
    public void Configure(EntityTypeBuilder<CounterStrikeTrainingSession> builder)
    {
        builder.ToTable("training_sessions", CounterStrikeConfiguration.Schema);
        builder.HasKey(session => session.Id);
        builder.HasIndex(session => new { session.OrganizationId, session.UserId, session.StartedAt })
            .HasDatabaseName("IX_cs_training_sessions_user");
    }
}

public sealed class CounterStrikeTrainingResultConfiguration : IEntityTypeConfiguration<CounterStrikeTrainingResult>
{
    public void Configure(EntityTypeBuilder<CounterStrikeTrainingResult> builder)
    {
        builder.ToTable("training_results", CounterStrikeConfiguration.Schema);
        builder.HasKey(result => result.Id);
        builder.HasIndex(result => new { result.OrganizationId, result.UserId, result.CompletedAt })
            .HasDatabaseName("IX_cs_training_results_user");
        builder.HasOne<CounterStrikeTrainingSession>().WithMany()
            .HasForeignKey(result => result.TrainingSessionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CounterStrikeWeeklyChallengeConfiguration : IEntityTypeConfiguration<CounterStrikeWeeklyChallenge>
{
    public void Configure(EntityTypeBuilder<CounterStrikeWeeklyChallenge> builder)
    {
        builder.ToTable("weekly_challenges", CounterStrikeConfiguration.Schema);
        builder.HasKey(challenge => challenge.Id);
        builder.HasIndex(challenge => new { challenge.OrganizationId, challenge.StartsAt, challenge.EndsAt })
            .HasDatabaseName("IX_cs_weekly_challenges_date");
        builder.Property(challenge => challenge.Name).HasMaxLength(160).IsRequired();
        builder.Property(challenge => challenge.Description).HasMaxLength(600).IsRequired();
        builder.Property(challenge => challenge.MetricKey).HasMaxLength(80).IsRequired();
    }
}

public sealed class CounterStrikeWeeklyChallengeProgressConfiguration
    : IEntityTypeConfiguration<CounterStrikeWeeklyChallengeProgress>
{
    public void Configure(EntityTypeBuilder<CounterStrikeWeeklyChallengeProgress> builder)
    {
        builder.ToTable("weekly_challenge_progress", CounterStrikeConfiguration.Schema);
        builder.HasKey(progress => progress.Id);
        builder.HasIndex(progress => new { progress.ChallengeId, progress.UserId })
            .IsUnique().HasDatabaseName("IX_cs_challenge_progress_user");
        builder.HasOne<CounterStrikeWeeklyChallenge>().WithMany()
            .HasForeignKey(progress => progress.ChallengeId).OnDelete(DeleteBehavior.Cascade);
    }
}
