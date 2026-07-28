using System.Text.Json;

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

public sealed record DashboardResponse(
    int MemberCount,
    int OpenTaskCount,
    int ActiveProjectCount,
    int OpenIncidentCount,
    CurrentAwardResponse? CurrentAward,
    IReadOnlyList<ActivityResponse> RecentActivities,
    string SystemMessage);
