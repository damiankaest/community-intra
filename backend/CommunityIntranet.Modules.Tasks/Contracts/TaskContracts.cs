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
    Guid? ConcurrencyToken = null,
    IReadOnlyList<CreateTaskMaterialRequest>? Materials = null);

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

public sealed record AddTaskCommentRequest(
    string? Body,
    IReadOnlyList<Guid>? MentionedMemberIds = null);

public sealed record CreateTaskMaterialRequest(
    string? Name,
    string? Quantity,
    string? Notes = null);

public sealed record ChangeTaskMaterialStateRequest(
    bool IsPrepared,
    Guid ConcurrencyToken);

public sealed record TaskMaterialResponse(
    Guid Id,
    Guid TaskId,
    string Name,
    string Quantity,
    string? Notes,
    bool IsPrepared,
    Guid? PreparedByMemberId,
    DateTimeOffset? PreparedAt,
    int SortOrder,
    Guid ConcurrencyToken);

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
    string ContentUrl,
    string? ThumbnailUrl);

public sealed record TaskDetailsResponse(
    TaskResponse Task,
    IReadOnlyList<TaskResponse> Subtasks,
    IReadOnlyList<TaskMaterialResponse> Materials,
    IReadOnlyList<TaskCommentResponse> Comments,
    IReadOnlyList<TaskAttachmentResponse> Attachments);
