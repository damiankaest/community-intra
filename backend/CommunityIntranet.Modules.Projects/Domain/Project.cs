namespace CommunityIntranet.Modules.Projects.Domain;

public sealed class Project
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; }

    public ProjectPriority Priority { get; set; }

    public Guid? OwnerMemberId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public Guid ConcurrencyToken { get; set; }
}

public enum ProjectStatus
{
    Idea,
    Planned,
    InProgress,
    Blocked,
    Completed,
    Cancelled
}

public enum ProjectPriority
{
    Low,
    Normal,
    High,
    Critical
}
