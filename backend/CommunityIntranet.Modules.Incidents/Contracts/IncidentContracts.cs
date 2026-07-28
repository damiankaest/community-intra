using CommunityIntranet.Modules.Incidents.Domain;

namespace CommunityIntranet.Modules.Incidents.Contracts;

public sealed record SaveIncidentRequest(
    string Title,
    string Description,
    string Category,
    IncidentSeverity Severity,
    IncidentStatus Status,
    Guid? ResponsibleMemberId,
    string? Resolution,
    string? LessonsLearned,
    DateTimeOffset OccurredAt,
    Guid? ConcurrencyToken = null);

public sealed record ResolveIncidentRequest(
    string Resolution,
    string? LessonsLearned,
    Guid ConcurrencyToken);

public sealed record IncidentResponse(
    Guid Id,
    string Title,
    string Description,
    string Category,
    IncidentSeverity Severity,
    IncidentStatus Status,
    Guid ReportedByMemberId,
    Guid? ResponsibleMemberId,
    string? Resolution,
    string? LessonsLearned,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt,
    Guid ConcurrencyToken);
