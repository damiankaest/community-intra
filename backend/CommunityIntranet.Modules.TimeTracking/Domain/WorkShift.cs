namespace CommunityIntranet.Modules.TimeTracking.Domain;

public sealed class WorkShift
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid MemberId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Guid ConcurrencyToken { get; set; }
}
