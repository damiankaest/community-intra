namespace CommunityIntranet.Modules.Tasks.Domain;

public sealed class TaskComment
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid TaskId { get; set; }

    public Guid AuthorMemberId { get; set; }

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
