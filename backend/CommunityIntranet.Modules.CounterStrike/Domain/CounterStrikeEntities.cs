namespace CommunityIntranet.Modules.CounterStrike.Domain;

public enum CounterStrikeDemoStatus
{
    Uploaded,
    Processing,
    Completed,
    Failed
}

public enum CounterStrikeAvailability
{
    Yes,
    Maybe,
    No
}

public enum CounterStrikePlayerRole
{
    Unset,
    Igl,
    Entry,
    Rifler,
    Awper,
    Support,
    Lurker
}

public enum CounterStrikeTrainingKind
{
    Flick,
    Reaction,
    TargetSwitching,
    Tracking,
    Utility,
    Teamplay
}

public sealed class CounterStrikeCommunitySettings
{
    public Guid OrganizationId { get; set; }
    public Guid? ActiveSeasonId { get; set; }
    public int DemoMaximumMegabytes { get; set; } = 512;
    public bool IsEnabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CounterStrikeSeason
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CounterStrikeMatch
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public Guid UploadedByMemberId { get; set; }
    public required string DemoChecksum { get; set; }
    public required string OriginalFileName { get; set; }
    public required string DemoStoragePath { get; set; }
    public string? AnalyzerArtifactPath { get; set; }
    public CounterStrikeDemoStatus Status { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public int AttemptCount { get; set; }
    public string? MapName { get; set; }
    public DateTimeOffset? PlayedAt { get; set; }
    public string? TeamAName { get; set; }
    public string? TeamBName { get; set; }
    public int TeamAScore { get; set; }
    public int TeamBScore { get; set; }
    public string? CommunityTeam { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? ProcessingStartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class CounterStrikeMatchPlayer
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MatchId { get; set; }
    public Guid? UserId { get; set; }
    public required string SteamId64 { get; set; }
    public required string DisplayName { get; set; }
    public required string TeamName { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public double Adr { get; set; }
    public double Kast { get; set; }
    public double HeadshotPercent { get; set; }
    public int UtilityDamage { get; set; }
    public int FirstKills { get; set; }
    public int FirstDeaths { get; set; }
    public int TradeKills { get; set; }
    public int BombPlants { get; set; }
    public int BombDefuses { get; set; }
    public double HltvRating { get; set; }
    public int ThreeKills { get; set; }
    public int FourKills { get; set; }
    public int Aces { get; set; }
    public int ClutchesWon { get; set; }
}

public sealed class CounterStrikeRound
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MatchId { get; set; }
    public int Number { get; set; }
    public int StartTick { get; set; }
    public int EndTick { get; set; }
    public required string WinnerTeam { get; set; }
    public int TeamAScore { get; set; }
    public int TeamBScore { get; set; }
}

public sealed class CounterStrikePlayerStats
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid UserId { get; set; }
    public int Matches { get; set; }
    public int Wins { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Assists { get; set; }
    public double Adr { get; set; }
    public double Kast { get; set; }
    public double HeadshotPercent { get; set; }
    public double HltvRating { get; set; }
    public int UtilityDamage { get; set; }
    public int FirstKills { get; set; }
    public int FirstDeaths { get; set; }
    public int TradeKills { get; set; }
    public int ThreeKills { get; set; }
    public int FourKills { get; set; }
    public int Aces { get; set; }
    public int ClutchesWon { get; set; }
    public CounterStrikePlayerRole Role { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CounterStrikeHighlight
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid MatchId { get; set; }
    public Guid? UserId { get; set; }
    public required string SteamId64 { get; set; }
    public required string PlayerName { get; set; }
    public int RoundNumber { get; set; }
    public required string Type { get; set; }
    public required string Title { get; set; }
    public int Score { get; set; }
    public int StartTick { get; set; }
    public int? EndTick { get; set; }
    public string? VideoStoragePath { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CounterStrikeHighlightReaction
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HighlightId { get; set; }
    public Guid UserId { get; set; }
    public required string Reaction { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CounterStrikeAward
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SeasonId { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Icon { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CounterStrikeAwardAssignment
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid AwardId { get; set; }
    public Guid UserId { get; set; }
    public double Value { get; set; }
    public DateTimeOffset AssignedAt { get; set; }
}

public sealed class CounterStrikeGameSession
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset SessionDate { get; set; }
    public TimeOnly? PlannedStart { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsClosed { get; set; }
}

public sealed class CounterStrikeGameSessionParticipant
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid GameSessionId { get; set; }
    public Guid UserId { get; set; }
    public CounterStrikeAvailability Availability { get; set; }
    public TimeOnly? AvailableFrom { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CounterStrikeTrainingPlan
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public DateOnly PlanDate { get; set; }
    public int PlannedMinutes { get; set; }
    public string? RecommendationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class CounterStrikeTrainingExercise
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? TrainingPlanId { get; set; }
    public CounterStrikeTrainingKind Kind { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public int DurationMinutes { get; set; }
    public string? MapName { get; set; }
    public string? Position { get; set; }
    public string? Target { get; set; }
    public string? MediaUrl { get; set; }
    public int SortOrder { get; set; }
}

public sealed class CounterStrikeTrainingSession
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? TrainingPlanId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int DurationSeconds { get; set; }
}

public sealed class CounterStrikeTrainingResult
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? TrainingSessionId { get; set; }
    public Guid? TrainingExerciseId { get; set; }
    public CounterStrikeTrainingKind Kind { get; set; }
    public int Hits { get; set; }
    public int Misses { get; set; }
    public double Accuracy { get; set; }
    public double ReactionTimeMs { get; set; }
    public double FlickTimeMs { get; set; }
    public double TrackingPercent { get; set; }
    public int Repetitions { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}

public sealed class CounterStrikeWeeklyChallenge
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SeasonId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string MetricKey { get; set; }
    public double TargetValue { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
}

public sealed class CounterStrikeWeeklyChallengeProgress
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ChallengeId { get; set; }
    public Guid UserId { get; set; }
    public double Value { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
