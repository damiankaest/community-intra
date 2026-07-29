using CommunityIntranet.Modules.AiAssistant.Contracts;

namespace CommunityIntranet.Modules.AiAssistant.Domain;

public sealed class AssistantConversation
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid MemberId { get; set; }

    public string? Title { get; set; }

    public AssistantTone Tone { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }
}

public sealed class AssistantMessage
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ConversationId { get; set; }

    public Guid MemberId { get; set; }

    public AssistantMessageRole Role { get; set; }

    public required string Content { get; set; }

    public string? Model { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public enum AssistantMessageRole
{
    User,
    Assistant
}
