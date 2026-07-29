using CommunityIntranet.Modules.Projects.Domain;

namespace CommunityIntranet.Modules.Projects.Contracts;

public sealed record SaveProjectRequest(
    string Name,
    string? Description,
    ProjectStatus Status,
    ProjectPriority Priority,
    Guid? OwnerMemberId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    Guid? ConcurrencyToken = null);

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    ProjectStatus Status,
    ProjectPriority Priority,
    Guid? OwnerMemberId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    Guid ConcurrencyToken);
