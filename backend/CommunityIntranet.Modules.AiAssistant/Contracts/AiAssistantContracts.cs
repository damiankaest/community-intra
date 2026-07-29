using System.Text.Json;
using CommunityIntranet.Modules.AiAssistant.Domain;
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

public sealed record SendAssistantMessageRequest(
    string? Message,
    AssistantTone Tone = AssistantTone.Theme);

public sealed record AssistantMessageResponse(
    Guid Id,
    AssistantMessageRole Role,
    string Content,
    DateTimeOffset CreatedAt);

public sealed record AssistantActionResponse(
    Guid Id,
    AssistantActionKind Kind,
    AssistantActionStatus Status,
    JsonElement Payload,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Guid? ResultEntityId,
    Guid ConcurrencyToken);

public sealed record AssistantConversationResponse(
    Guid? Id,
    AssistantTone Tone,
    IReadOnlyList<AssistantMessageResponse> Messages,
    IReadOnlyList<AssistantActionResponse> Actions);

public sealed record ConfirmAssistantActionRequest(Guid ConcurrencyToken);

public sealed record ConfirmedAssistantActionResponse(
    Guid ActionId,
    AssistantActionKind Kind,
    Guid ResultEntityId,
    bool AlreadyConfirmed);

public sealed record CreateTaskActionPayload(
    string Title,
    string? Description,
    Guid? ProjectId,
    Guid? ParentTaskId,
    WorkTaskPriority Priority,
    DateOnly? DueDate);

public sealed record UpdateTaskActionPayload(
    Guid TaskId,
    string? Title,
    string? Description,
    WorkTaskStatus? Status,
    WorkTaskPriority? Priority,
    Guid? AssignedMemberId,
    DateOnly? DueDate);

public sealed record CreateProjectActionPayload(
    string Name,
    string? Description,
    CommunityIntranet.Modules.Projects.Domain.ProjectPriority Priority);
