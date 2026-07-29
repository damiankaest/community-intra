using CommunityIntranet.Modules.Tasks.Domain;

namespace CommunityIntranet.Modules.AiAssistant.Contracts;

public enum AssistantTone
{
    Theme,
    Neutral
}

public sealed record PrepareWorkPlanRequest(
    string? Prompt,
    AssistantTone Tone = AssistantTone.Theme);

public sealed record ConfirmWorkPlanRequest(Guid ConcurrencyToken);

public sealed record WorkPlanMaterial(
    string Name,
    string Quantity,
    string? Notes);

public sealed record WorkPlanTask(
    string Title,
    string Description,
    WorkTaskPriority Priority,
    IReadOnlyList<string> AcceptanceCriteria);

public sealed record WorkPlanProposal(
    string Title,
    string ExecutiveSummary,
    string ManagementMessage,
    IReadOnlyList<WorkPlanMaterial> Materials,
    IReadOnlyList<WorkPlanTask> Tasks);

public sealed record WorkPlanDraftResponse(
    Guid Id,
    AssistantTone Tone,
    string Prompt,
    WorkPlanProposal Proposal,
    string Model,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ConfirmedAt,
    Guid? ProjectId,
    Guid ConcurrencyToken);

public sealed record ConfirmedWorkPlanResponse(
    Guid DraftId,
    Guid ProjectId,
    IReadOnlyList<Guid> TaskIds,
    bool AlreadyConfirmed);

public sealed record AiAssistantAvailabilityResponse(
    bool IsConfigured,
    string Model);
