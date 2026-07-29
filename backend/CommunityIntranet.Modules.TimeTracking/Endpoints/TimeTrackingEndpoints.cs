using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.TimeTracking.Contracts;
using CommunityIntranet.Modules.TimeTracking.Domain;
using CommunityIntranet.Modules.TimeTracking.Persistence;
using CommunityIntranet.Modules.TimeTracking.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.TimeTracking.Endpoints;

public static class TimeTrackingEndpoints
{
    public static IEndpointRouteBuilder MapTimeTrackingEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/time-clock")
            .WithTags("Time Tracking")
            .RequireAuthorization();
        group.MapGet("", GetOverviewAsync);
        group.MapPost("/clock-in", ClockInAsync);
        group.MapPost("/clock-out", ClockOutAsync);
        group.MapPost("/entries", CreateEntryAsync);
        return endpoints;
    }

    private static async Task<IResult> GetOverviewAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ITimeTrackingDbContext dbContext,
        IOrganizationAccessService accessService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var memberId = access.Membership!.MemberId;
        var now = timeProvider.GetUtcNow();
        var todayStart = new DateTimeOffset(
            now.UtcDateTime.Date,
            TimeSpan.Zero);
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        var weekStart = todayStart.AddDays(-daysSinceMonday);
        var weekShifts = await dbContext.WorkShifts
            .AsNoTracking()
            .Where(shift =>
                shift.OrganizationId == organizationId
                && shift.StartedAt <= now
                && (shift.EndedAt == null || shift.EndedAt >= weekStart))
            .OrderByDescending(shift => shift.StartedAt)
            .ToArrayAsync(cancellationToken);
        var activeShift = weekShifts.FirstOrDefault(shift =>
            shift.MemberId == memberId && shift.EndedAt == null);
        var todaySeconds = weekShifts
            .Where(shift => shift.MemberId == memberId)
            .Sum(shift => OverlapSeconds(shift, todayStart, now));
        var weekSeconds = weekShifts
            .Where(shift => shift.MemberId == memberId)
            .Sum(shift => OverlapSeconds(shift, weekStart, now));

        var memberIds = weekShifts
            .Select(shift => shift.MemberId)
            .Distinct()
            .ToArray();
        var displayNames = new Dictionary<Guid, string?>();
        foreach (var id in memberIds)
        {
            displayNames[id] = await accessService.GetMemberDisplayNameAsync(
                organizationId,
                id,
                cancellationToken);
        }

        var activeMembers = weekShifts
            .Where(shift => shift.EndedAt == null)
            .Select(shift => new ActiveMemberResponse(
                shift.MemberId,
                displayNames.GetValueOrDefault(shift.MemberId),
                shift.StartedAt,
                ElapsedSeconds(shift, now)))
            .OrderBy(member => member.DisplayName)
            .ToArray();
        var leaderboard = weekShifts
            .GroupBy(shift => shift.MemberId)
            .Select(group => new MemberTimeSummaryResponse(
                group.Key,
                displayNames.GetValueOrDefault(group.Key),
                group.Sum(shift => OverlapSeconds(shift, weekStart, now))))
            .Where(summary => summary.ElapsedSeconds > 0)
            .OrderByDescending(summary => summary.ElapsedSeconds)
            .ThenBy(summary => summary.DisplayName)
            .Take(10)
            .ToArray();

        var entries = await dbContext.WorkLogEntries
            .AsNoTracking()
            .Where(entry => entry.OrganizationId == organizationId)
            .OrderByDescending(entry => entry.CreatedAt)
            .Take(30)
            .ToArrayAsync(cancellationToken);
        foreach (var id in entries
                     .Select(entry => entry.MemberId)
                     .Distinct()
                     .Where(id => !displayNames.ContainsKey(id)))
        {
            displayNames[id] = await accessService.GetMemberDisplayNameAsync(
                organizationId,
                id,
                cancellationToken);
        }

        var recentShifts = await dbContext.WorkShifts
            .AsNoTracking()
            .Where(shift =>
                shift.OrganizationId == organizationId
                && shift.MemberId == memberId)
            .OrderByDescending(shift => shift.StartedAt)
            .Take(12)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(new TimeClockOverviewResponse(
            now,
            activeShift is null ? null : ToResponse(activeShift, now),
            todaySeconds,
            weekSeconds,
            activeMembers,
            leaderboard,
            entries.Select(entry => new WorkLogEntryResponse(
                entry.Id,
                entry.MemberId,
                displayNames.GetValueOrDefault(entry.MemberId),
                entry.Kind,
                entry.Note,
                entry.CreatedAt)).ToArray(),
            recentShifts.Select(shift => ToResponse(shift, now)).ToArray()));
    }

    private static async Task<IResult> ClockInAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        ITimeClockService timeClockService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var result = await timeClockService.ClockInAsync(
            organizationId,
            access.Membership!.MemberId,
            cancellationToken);
        return Results.Ok(new ClockInResponse(
            ToResponse(result.Shift, timeProvider.GetUtcNow()),
            result.AlreadyActive));
    }

    private static async Task<IResult> ClockOutAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        ITimeClockService timeClockService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var result = await timeClockService.ClockOutAsync(
            organizationId,
            access.Membership!.MemberId,
            cancellationToken);
        return result.Shift is null
            ? Results.Conflict(new
            {
                title = "Keine laufende Schicht",
                detail = result.Error
            })
            : Results.Ok(ToResponse(
                result.Shift,
                timeProvider.GetUtcNow()));
    }

    private static async Task<IResult> CreateEntryAsync(
        Guid organizationId,
        CreateWorkLogEntryRequest request,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        ITimeClockService timeClockService,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var note = request.Note?.Trim();
        if (!Enum.IsDefined(request.Kind)
            || string.IsNullOrWhiteSpace(note)
            || note.Length > 240)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["entry"] =
                [
                    "Art und Beschreibung mit maximal 240 Zeichen sind erforderlich."
                ]
            });
        }

        var memberId = access.Membership!.MemberId;
        var result = await timeClockService.LogWorkAsync(
            organizationId,
            memberId,
            request.Kind,
            note,
            cancellationToken);
        if (result.Entry is null)
        {
            return Results.Conflict(new
            {
                title = "Nicht eingestempelt",
                detail = result.Error
            });
        }

        return Results.Created(
            $"/api/organizations/{organizationId}/time-clock/entries/{result.Entry.Id}",
            new WorkLogEntryResponse(
                result.Entry.Id,
                result.Entry.MemberId,
                await accessService.GetMemberDisplayNameAsync(
                    organizationId,
                    memberId,
                    cancellationToken),
                result.Entry.Kind,
                result.Entry.Note,
                result.Entry.CreatedAt));
    }

    private static WorkShiftResponse ToResponse(
        WorkShift shift,
        DateTimeOffset now) =>
        new(
            shift.Id,
            shift.MemberId,
            shift.StartedAt,
            shift.EndedAt,
            ElapsedSeconds(shift, now));

    private static long ElapsedSeconds(
        WorkShift shift,
        DateTimeOffset now) =>
        Math.Max(
            0,
            (long)Math.Floor(((shift.EndedAt ?? now) - shift.StartedAt)
                .TotalSeconds));

    private static long OverlapSeconds(
        WorkShift shift,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd)
    {
        var start = shift.StartedAt > rangeStart
            ? shift.StartedAt
            : rangeStart;
        var shiftEnd = shift.EndedAt ?? rangeEnd;
        var end = shiftEnd < rangeEnd ? shiftEnd : rangeEnd;
        return end <= start
            ? 0
            : (long)Math.Floor((end - start).TotalSeconds);
    }

    private static async Task<AccessResult> GetAccessAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var rawUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(rawUserId, out var userId))
        {
            return new AccessResult(null, Results.Unauthorized());
        }

        var membership = await accessService.GetActiveMembershipAsync(
            organizationId,
            userId,
            cancellationToken);
        return membership is null
            ? new AccessResult(null, Results.NotFound())
            : new AccessResult(membership, null);
    }

    private sealed record AccessResult(
        OrganizationMembership? Membership,
        IResult? Result);
}
