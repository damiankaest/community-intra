using CommunityIntranet.Modules.Tasks.Domain;

namespace CommunityIntranet.Modules.Tasks.Contracts;

public sealed record SaveTaskRequest(
    string Title,
    string? Description,
    WorkTaskStatus Status,
    WorkTaskPriority Priority,
    Guid? ProjectId,
    Guid? AssignedMemberId,
    DateOnly? DueDate,
    Guid? ConcurrencyToken = null);

public sealed record ChangeTaskStatusRequest(
    WorkTaskStatus Status,
    Guid ConcurrencyToken);

public sealed record TaskResponse(
    Guid Id,
    Guid? ProjectId,
    string Title,
    string? Description,
    WorkTaskStatus Status,
    WorkTaskPriority Priority,
    Guid? AssignedMemberId,
    Guid CreatedByMemberId,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    Guid ConcurrencyToken);
