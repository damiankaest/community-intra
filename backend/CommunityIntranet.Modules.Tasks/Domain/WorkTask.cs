namespace CommunityIntranet.Modules.Tasks.Domain;

public sealed class WorkTask
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid? ProjectId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public WorkTaskStatus Status { get; set; }

    public WorkTaskPriority Priority { get; set; }

    public Guid? AssignedMemberId { get; set; }

    public Guid CreatedByMemberId { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public Guid ConcurrencyToken { get; set; }
}

public enum WorkTaskStatus
{
    Open,
    InProgress,
    Blocked,
    Done,
    Cancelled
}

public enum WorkTaskPriority
{
    Low,
    Normal,
    High,
    Critical
}
