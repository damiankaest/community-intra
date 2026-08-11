using CommunityIntranet.BuildingBlocks.Tenancy;

namespace CommunityIntranet.Modules.Football.Domain;

public enum FootballLiveTrainingStatus
{
    NotStarted = 0,
    Running = 10,
    Paused = 20,
    Completed = 30
}

public sealed class FootballLiveTrainingRun : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SessionId { get; set; }
    public FootballLiveTrainingStatus Status { get; set; } = FootballLiveTrainingStatus.NotStarted;
    public Guid? ActiveTrainingBlockId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? PausedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int AccumulatedPausedSeconds { get; set; }
    public Guid UpdatedByMemberId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class FootballLiveTrainingBlockRun : IOrganizationScoped
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SessionId { get; set; }
    public Guid TrainingBlockId { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? PausedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int AccumulatedSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
