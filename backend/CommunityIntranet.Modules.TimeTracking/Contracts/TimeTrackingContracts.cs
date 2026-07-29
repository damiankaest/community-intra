using CommunityIntranet.Modules.TimeTracking.Domain;

namespace CommunityIntranet.Modules.TimeTracking.Contracts;

public sealed record CreateWorkLogEntryRequest(
    WorkLogKind Kind,
    string? Note);

public sealed record WorkShiftResponse(
    Guid Id,
    Guid MemberId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    long ElapsedSeconds);

public sealed record WorkLogEntryResponse(
    Guid Id,
    Guid MemberId,
    string? MemberDisplayName,
    WorkLogKind Kind,
    string Note,
    DateTimeOffset CreatedAt);

public sealed record ActiveMemberResponse(
    Guid MemberId,
    string? DisplayName,
    DateTimeOffset StartedAt,
    long ElapsedSeconds);

public sealed record MemberTimeSummaryResponse(
    Guid MemberId,
    string? DisplayName,
    long ElapsedSeconds);

public sealed record TimeClockOverviewResponse(
    DateTimeOffset CheckedAt,
    WorkShiftResponse? ActiveShift,
    long TodaySeconds,
    long WeekSeconds,
    IReadOnlyList<ActiveMemberResponse> ActiveMembers,
    IReadOnlyList<MemberTimeSummaryResponse> WeeklyLeaderboard,
    IReadOnlyList<WorkLogEntryResponse> RecentEntries,
    IReadOnlyList<WorkShiftResponse> RecentShifts);

public sealed record ClockInResponse(
    WorkShiftResponse Shift,
    bool AlreadyActive);
