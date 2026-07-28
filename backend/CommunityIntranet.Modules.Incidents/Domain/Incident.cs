namespace CommunityIntranet.Modules.Incidents.Domain;

public sealed class Incident
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string Category { get; set; }

    public IncidentSeverity Severity { get; set; }

    public IncidentStatus Status { get; set; }

    public Guid ReportedByMemberId { get; set; }

    public Guid? ResponsibleMemberId { get; set; }

    public string? Resolution { get; set; }

    public string? LessonsLearned { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ResolvedAt { get; set; }

    public Guid ConcurrencyToken { get; set; }
}

public enum IncidentSeverity
{
    Informational,
    Low,
    Medium,
    High,
    Catastrophic
}

public enum IncidentStatus
{
    Reported,
    UnderInvestigation,
    Resolved,
    Rejected
}
