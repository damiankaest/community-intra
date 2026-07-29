using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.Modules.TimeTracking.Domain;
using CommunityIntranet.Modules.TimeTracking.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.TimeTracking.Services;

public sealed class TimeClockService(
    ITimeTrackingDbContext dbContext,
    IActivityWriter activityWriter,
    TimeProvider timeProvider) : ITimeClockService
{
    public async Task<ClockInResult> ClockInAsync(
        Guid organizationId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var active = await FindActiveShiftAsync(
            organizationId,
            memberId,
            cancellationToken);
        if (active is not null)
        {
            return new ClockInResult(active, true);
        }

        var now = timeProvider.GetUtcNow();
        var shift = new WorkShift
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            MemberId = memberId,
            StartedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.WorkShifts.Add(shift);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "time_clock.clocked_in",
            memberId,
            "work_shift",
            shift.Id,
            new Dictionary<string, string?>()));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ClockInResult(shift, false);
    }

    public async Task<ClockOutResult> ClockOutAsync(
        Guid organizationId,
        Guid memberId,
        CancellationToken cancellationToken)
    {
        var active = await FindActiveShiftAsync(
            organizationId,
            memberId,
            cancellationToken);
        if (active is null)
        {
            return new ClockOutResult(
                null,
                "Du bist gerade nicht eingestempelt.");
        }

        var now = timeProvider.GetUtcNow();
        active.EndedAt = now;
        active.UpdatedAt = now;
        active.ConcurrencyToken = Guid.NewGuid();
        var elapsedMinutes = Math.Max(
            0,
            (long)Math.Floor((now - active.StartedAt).TotalMinutes));
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "time_clock.clocked_out",
            memberId,
            "work_shift",
            active.Id,
            new Dictionary<string, string?>
            {
                ["elapsedMinutes"] = elapsedMinutes.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new ClockOutResult(active, null);
    }

    public async Task<LogWorkResult> LogWorkAsync(
        Guid organizationId,
        Guid memberId,
        WorkLogKind kind,
        string note,
        CancellationToken cancellationToken)
    {
        var active = await FindActiveShiftAsync(
            organizationId,
            memberId,
            cancellationToken);
        if (active is null)
        {
            return new LogWorkResult(
                null,
                "Stempel dich zuerst ein, bevor du etwas ins Logbuch schreibst.");
        }

        var entry = new WorkLogEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            MemberId = memberId,
            WorkShiftId = active.Id,
            Kind = kind,
            Note = note,
            CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.WorkLogEntries.Add(entry);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "time_clock.work_logged",
            memberId,
            "work_log_entry",
            entry.Id,
            new Dictionary<string, string?>
            {
                ["kind"] = kind.ToString(),
                ["note"] = note
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return new LogWorkResult(entry, null);
    }

    private Task<WorkShift?> FindActiveShiftAsync(
        Guid organizationId,
        Guid memberId,
        CancellationToken cancellationToken) =>
        dbContext.WorkShifts.SingleOrDefaultAsync(
            shift =>
                shift.OrganizationId == organizationId
                && shift.MemberId == memberId
                && shift.EndedAt == null,
            cancellationToken);
}
