namespace CommunityIntranet.Modules.TimeTracking.Domain;

public sealed class WorkLogEntry
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid MemberId { get; set; }

    public Guid WorkShiftId { get; set; }

    public WorkLogKind Kind { get; set; }

    public required string Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public enum WorkLogKind
{
    Built,
    Fixed,
    Optimized,
    Destroyed
}
