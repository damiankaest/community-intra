using CommunityIntranet.Modules.TimeTracking.Domain;

namespace CommunityIntranet.Modules.TimeTracking.Services;

public interface ITimeClockService
{
    Task<ClockInResult> ClockInAsync(
        Guid organizationId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<ClockOutResult> ClockOutAsync(
        Guid organizationId,
        Guid memberId,
        CancellationToken cancellationToken);

    Task<LogWorkResult> LogWorkAsync(
        Guid organizationId,
        Guid memberId,
        WorkLogKind kind,
        string note,
        CancellationToken cancellationToken);
}

public sealed record ClockInResult(WorkShift Shift, bool AlreadyActive);

public sealed record ClockOutResult(WorkShift? Shift, string? Error);

public sealed record LogWorkResult(WorkLogEntry? Entry, string? Error);
