using CommunityIntranet.Modules.AiAssistant.Contracts;

namespace CommunityIntranet.Modules.AiAssistant.Domain;

public sealed class WorkPlanDraft
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid CreatedByMemberId { get; set; }

    public required string Prompt { get; set; }

    public AssistantTone Tone { get; set; }

    public required string ProposalJson { get; set; }

    public required string Model { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid ConcurrencyToken { get; set; }
}
