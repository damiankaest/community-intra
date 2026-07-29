namespace CommunityIntranet.Modules.AiAssistant.Domain;

public sealed class AssistantAction
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ConversationId { get; set; }

    public Guid RequestedByMemberId { get; set; }

    public AssistantActionKind Kind { get; set; }

    public required string PayloadJson { get; set; }

    public AssistantActionStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public Guid? ResultEntityId { get; set; }

    public Guid ConcurrencyToken { get; set; }
}

public enum AssistantActionKind
{
    CreateTask,
    UpdateTask,
    CreateProject,
    AddTaskComment
}

public enum AssistantActionStatus
{
    Pending,
    Confirmed,
    Rejected
}
