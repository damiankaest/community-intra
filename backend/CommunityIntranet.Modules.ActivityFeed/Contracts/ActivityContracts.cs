using System.Text.Json;
using CommunityIntranet.Modules.Projects.Domain;
using CommunityIntranet.Modules.Tasks.Domain;

namespace CommunityIntranet.Modules.ActivityFeed.Contracts;

public sealed record ActivityResponse(
    Guid Id,
    string ActivityType,
    Guid ActorMemberId,
    string? ActorDisplayName,
    string EntityType,
    Guid EntityId,
    JsonElement Data,
    int EventVersion,
    DateTimeOffset CreatedAt);

public sealed record CurrentAwardResponse(
    Guid Id,
    string Name,
    string Description,
    string? AwardedToDisplayName,
    DateTimeOffset AwardedAt,
    string Icon,
    string Category);

public sealed record DashboardFocusProjectResponse(
    Guid Id,
    string Name,
    ProjectStatus Status,
    ProjectPriority Priority,
    int CompletedTaskCount,
    int TotalTaskCount);

public sealed record DashboardTaskResponse(
    Guid Id,
    string Title,
    WorkTaskStatus Status,
    WorkTaskPriority Priority,
    string? AssignedToDisplayName,
    DateOnly? DueDate,
    int PreparedMaterialCount,
    int MaterialCount);

public sealed record WeeklyPulseResponse(
    int CreatedTaskCount,
    int CompletedTaskCount,
    int CommentCount,
    int ScreenshotCount,
    int ActiveContributorCount);

public sealed record DashboardResponse(
    int MemberCount,
    int OpenTaskCount,
    int ActiveProjectCount,
    int OpenIncidentCount,
    DashboardFocusProjectResponse? FocusProject,
    IReadOnlyList<DashboardTaskResponse> PriorityTasks,
    WeeklyPulseResponse WeeklyPulse,
    CurrentAwardResponse? CurrentAward,
    IReadOnlyList<ActivityResponse> RecentActivities,
    string SystemMessage);
