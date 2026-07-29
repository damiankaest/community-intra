using CommunityIntranet.Modules.Tasks.Domain;

namespace CommunityIntranet.Modules.Tasks.Contracts;

public sealed record SaveTaskRequest(
    string Title,
    string? Description,
    WorkTaskStatus Status,
    WorkTaskPriority Priority,
    Guid? ProjectId,
    Guid? ParentTaskId,
    Guid? AssignedMemberId,
    DateOnly? DueDate,
    Guid? ConcurrencyToken = null);

public sealed record ChangeTaskStatusRequest(
    WorkTaskStatus Status,
    Guid ConcurrencyToken);

public sealed record TaskResponse(
    Guid Id,
    Guid? ProjectId,
    Guid? ParentTaskId,
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

public sealed record AddTaskCommentRequest(string? Body);

public sealed record TaskCommentResponse(
    Guid Id,
    Guid TaskId,
    Guid AuthorMemberId,
    string? AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record TaskAttachmentResponse(
    Guid Id,
    Guid TaskId,
    Guid UploadedByMemberId,
    string? UploadedByDisplayName,
    string FileName,
    string MediaType,
    long Size,
    DateTimeOffset CreatedAt,
    string ContentUrl);

public sealed record TaskDetailsResponse(
    TaskResponse Task,
    IReadOnlyList<TaskResponse> Subtasks,
    IReadOnlyList<TaskCommentResponse> Comments,
    IReadOnlyList<TaskAttachmentResponse> Attachments);
