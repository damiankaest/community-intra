using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Football.Domain;
using CommunityIntranet.Modules.Football.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Football.Endpoints;

public static class FootballEndpoints
{
    public static IEndpointRouteBuilder MapFootballEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations/{organizationId:guid}/football")
            .WithTags("Football")
            .RequireAuthorization();

        group.MapGet("/profiles", ListProfilesAsync);
        group.MapPut("/profiles/{memberId:guid}", UpsertProfileAsync);
        group.MapGet("/availability", ListAvailabilityAsync);
        group.MapPut("/availability/{memberId:guid}", UpsertAvailabilityAsync);
        group.MapGet("/exercises", ListExercisesAsync);
        group.MapPost("/exercises", CreateExerciseAsync);
        group.MapGet("/sessions", ListSessionsAsync);
        group.MapPost("/sessions", CreateSessionAsync);
        group.MapGet("/sessions/{sessionId:guid}", GetSessionAsync);
        group.MapPut("/sessions/{sessionId:guid}/attendance/{memberId:guid}", UpdateAttendanceAsync);
        group.MapPut("/sessions/{sessionId:guid}/load/{memberId:guid}", UpsertSessionLoadAsync);
        group.MapPut("/sessions/{sessionId:guid}/blocks", ReplaceTrainingBlocksAsync);
        group.MapGet("/sessions/{sessionId:guid}/feedback", GetSessionFeedbackAsync);
        group.MapPut("/sessions/{sessionId:guid}/exercises/{exerciseId:guid}/feedback/{memberId:guid}", UpsertExerciseFeedbackAsync);
        group.MapGet("/members/{memberId:guid}/history", GetMemberTrainingHistoryAsync);
        return endpoints;
    }

    private static async Task<IResult> ListProfilesAsync(Guid organizationId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;

        var profiles = await db.FootballMemberProfiles.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.TeamRole).ThenBy(x => x.Position)
            .ToArrayAsync(ct);
        return Results.Ok(profiles);
    }

    private static async Task<IResult> UpsertProfileAsync(Guid organizationId, Guid memberId, UpsertFootballProfileRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership.Result is not null) return membership.Result;
        if (!await access.IsActiveMemberAsync(organizationId, memberId, ct)) return Results.NotFound();
        if (!Enum.IsDefined(request.TeamRole) || (request.Position is not null && !Enum.IsDefined(request.Position.Value)))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["profile"] = ["Ungültige Fußballrolle oder Position."] });

        var entity = await db.FootballMemberProfiles.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.MemberId == memberId, ct);
        if (entity is null)
        {
            entity = new FootballMemberProfile { Id = Guid.NewGuid(), OrganizationId = organizationId, MemberId = memberId };
            db.FootballMemberProfiles.Add(entity);
        }

        entity.TeamRole = request.TeamRole;
        entity.Position = request.Position;
        entity.ShirtNumber = request.ShirtNumber;
        entity.Description = Clean(request.Description, 1000);
        entity.Strengths = CleanArray(request.Strengths, 12, 80);
        entity.DevelopmentAreas = CleanArray(request.DevelopmentAreas, 12, 80);
        entity.SecondaryPositions = CleanArray(request.SecondaryPositions, 4, 40);
        entity.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Results.Ok(entity);
    }

    private static async Task<IResult> ListAvailabilityAsync(Guid organizationId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;

        return Results.Ok(await db.FootballPlayerAvailability.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.Status)
            .ThenBy(x => x.MemberId)
            .ToArrayAsync(ct));
    }

    private static async Task<IResult> UpsertAvailabilityAsync(Guid organizationId, Guid memberId, UpdateFootballAvailabilityRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        if (!await access.IsActiveMemberAsync(organizationId, memberId, ct)) return Results.NotFound();
        if (!Enum.IsDefined(request.Status) || request.MaxLoadPercent is < 0 or > 100)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["availability"] = ["Status oder maximale Belastung ist ungültig."] });

        var isCoach = await CanCoachAsync(organizationId, membership.Membership!, db, ct);
        if (membership.Membership!.MemberId != memberId && !isCoach) return Results.Forbid();

        var entity = await db.FootballPlayerAvailability.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.MemberId == memberId, ct);
        if (entity is null)
        {
            entity = new FootballPlayerAvailability { Id = Guid.NewGuid(), OrganizationId = organizationId, MemberId = memberId };
            db.FootballPlayerAvailability.Add(entity);
        }

        entity.Status = request.Status;
        entity.MaxLoadPercent = request.Status == FootballAvailabilityStatus.Injured ? 0 : request.MaxLoadPercent;
        entity.Note = Clean(request.Note, 500);
        entity.UpdatedAt = clock.GetUtcNow();
        entity.UpdatedByMemberId = membership.Membership.MemberId;
        await db.SaveChangesAsync(ct);
        return Results.Ok(entity);
    }

    private static async Task<IResult> ListExercisesAsync(Guid organizationId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, FootballExerciseCategory? category, int? players, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        var query = db.FootballExercises.AsNoTracking().Where(x => x.OrganizationId == organizationId && !x.IsArchived);
        if (category is not null) query = query.Where(x => x.Category == category);
        if (players is > 0) query = query.Where(x => x.MinPlayers <= players && (x.MaxPlayers == null || x.MaxPlayers >= players));
        return Results.Ok(await query.OrderBy(x => x.Category).ThenBy(x => x.Title).ToArrayAsync(ct));
    }

    private static async Task<IResult> CreateExerciseAsync(Guid organizationId, CreateFootballExerciseRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership.Result is not null) return membership.Result;
        if (string.IsNullOrWhiteSpace(request.Title) || request.MinPlayers < 1 || request.DefaultDurationMinutes is < 1 or > 240 || (request.MaxPlayers is not null && request.MaxPlayers < request.MinPlayers))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["exercise"] = ["Titel, Spielerzahl oder Dauer ist ungültig."] });

        var now = clock.GetUtcNow();
        var exercise = new FootballExercise
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title.Trim(),
            Description = Clean(request.Description, 3000) ?? string.Empty,
            Category = request.Category,
            Location = request.Location,
            Intensity = request.Intensity,
            MinPlayers = request.MinPlayers,
            MaxPlayers = request.MaxPlayers,
            DefaultDurationMinutes = request.DefaultDurationMinutes,
            Focus = Clean(request.Focus, 1000) ?? string.Empty,
            Equipment = CleanArray(request.Equipment, 20, 80),
            Tags = CleanArray(request.Tags, 20, 80),
            CreatedByMemberId = membership.Membership!.MemberId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.FootballExercises.Add(exercise);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/organizations/{organizationId}/football/exercises/{exercise.Id}", exercise);
    }

    private static async Task<IResult> ListSessionsAsync(Guid organizationId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        var query = db.FootballSessions.AsNoTracking().Where(x => x.OrganizationId == organizationId && !x.IsCancelled);
        if (from is not null) query = query.Where(x => x.StartsAt >= from);
        if (to is not null) query = query.Where(x => x.StartsAt <= to);
        return Results.Ok(await query.OrderBy(x => x.StartsAt).Take(100).ToArrayAsync(ct));
    }

    private static async Task<IResult> CreateSessionAsync(Guid organizationId, CreateFootballSessionRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership.Result is not null) return membership.Result;
        if (string.IsNullOrWhiteSpace(request.Title) || request.DurationMinutes is < 5 or > 480)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["session"] = ["Titel oder Dauer ist ungültig."] });

        var now = clock.GetUtcNow();
        var session = new FootballSession
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Kind = request.Kind, Title = request.Title.Trim(), Focus = Clean(request.Focus, 1000),
            Location = Clean(request.Location, 300), Opponent = Clean(request.Opponent, 180), StartsAt = request.StartsAt, DurationMinutes = request.DurationMinutes,
            CreatedByMemberId = membership.Membership!.MemberId, CreatedAt = now, UpdatedAt = now
        };
        db.FootballSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/organizations/{organizationId}/football/sessions/{session.Id}", session);
    }

    private static async Task<IResult> GetSessionAsync(Guid organizationId, Guid sessionId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        var session = await db.FootballSessions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct);
        if (session is null) return Results.NotFound();
        var attendance = await db.FootballAttendances.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).ToArrayAsync(ct);
        var blocks = await db.FootballTrainingBlocks.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).OrderBy(x => x.SortOrder).ToArrayAsync(ct);
        var load = await db.FootballSessionLoads.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).ToArrayAsync(ct);
        var availability = await db.FootballPlayerAvailability.AsNoTracking().Where(x => x.OrganizationId == organizationId).ToArrayAsync(ct);
        return Results.Ok(new { session, attendance, blocks, load, availability });
    }

    private static async Task<IResult> UpdateAttendanceAsync(Guid organizationId, Guid sessionId, Guid memberId, UpdateFootballAttendanceRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        var isCoach = await CanCoachAsync(organizationId, membership.Membership!, db, ct);
        if (membership.Membership!.MemberId != memberId && !isCoach) return Results.Forbid();
        if (!await access.IsActiveMemberAsync(organizationId, memberId, ct)) return Results.NotFound();
        if (!await db.FootballSessions.AnyAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct)) return Results.NotFound();

        var entity = await db.FootballAttendances.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.MemberId == memberId, ct);
        if (entity is null)
        {
            entity = new FootballAttendance { Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId, MemberId = memberId };
            db.FootballAttendances.Add(entity);
        }
        entity.Status = request.Status;
        entity.Note = Clean(request.Note, 500);
        entity.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Results.Ok(entity);
    }

    private static async Task<IResult> UpsertSessionLoadAsync(Guid organizationId, Guid sessionId, Guid memberId, UpdateFootballSessionLoadRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        var isCoach = await CanCoachAsync(organizationId, membership.Membership!, db, ct);
        if (membership.Membership!.MemberId != memberId && !isCoach) return Results.Forbid();
        if (!await access.IsActiveMemberAsync(organizationId, memberId, ct)) return Results.NotFound();
        if (request.Rpe is < 1 or > 10)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["rpe"] = ["RPE muss zwischen 1 und 10 liegen."] });

        var session = await db.FootballSessions.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct);
        if (session is null) return Results.NotFound();
        if (request.MinutesCompleted is < 0 || request.MinutesCompleted > session.DurationMinutes)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["minutesCompleted"] = ["Absolvierte Minuten müssen innerhalb der Termindauer liegen."] });

        var entity = await db.FootballSessionLoads.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.MemberId == memberId, ct);
        if (entity is null)
        {
            entity = new FootballSessionLoad { Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId, MemberId = memberId };
            db.FootballSessionLoads.Add(entity);
        }
        entity.Rpe = request.Rpe;
        entity.MinutesCompleted = request.MinutesCompleted;
        entity.Note = Clean(request.Note, 500);
        entity.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Results.Ok(entity);
    }

    private static async Task<IResult> ReplaceTrainingBlocksAsync(Guid organizationId, Guid sessionId, ReplaceFootballTrainingBlocksRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership.Result is not null) return membership.Result;
        var session = await db.FootballSessions.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct);
        if (session is null) return Results.NotFound();
        if (request.Blocks.Count > 30 || request.Blocks.Any(x => string.IsNullOrWhiteSpace(x.Title) || x.DurationMinutes is < 1 or > 180))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["blocks"] = ["Trainingsblöcke sind ungültig."] });

        var existing = await db.FootballTrainingBlocks.Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).ToArrayAsync(ct);
        db.FootballTrainingBlocks.RemoveRange(existing);
        var blocks = request.Blocks.Select((x, index) => new FootballTrainingBlock
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId, ExerciseId = x.ExerciseId, Title = x.Title.Trim(),
            Description = Clean(x.Description, 2000), CoachingPoints = Clean(x.CoachingPoints, 2000), SortOrder = index, DurationMinutes = x.DurationMinutes,
            ResponsibleMemberId = x.ResponsibleMemberId, AiReason = Clean(x.AiReason, 1500)
        }).ToArray();
        db.FootballTrainingBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);
        return Results.Ok(blocks);
    }

    private static async Task<IResult> GetSessionFeedbackAsync(Guid organizationId, Guid sessionId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        if (!await db.FootballSessions.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct)) return Results.NotFound();

        var feedback = await db.FootballExerciseFeedback.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId)
            .OrderBy(x => x.ExerciseId).ThenBy(x => x.MemberId)
            .ToArrayAsync(ct);

        var summary = feedback
            .GroupBy(x => x.ExerciseId)
            .Select(group => new
            {
                exerciseId = group.Key,
                count = group.Count(),
                fun = Math.Round(group.Average(x => x.Fun), 2),
                difficulty = Math.Round(group.Average(x => x.Difficulty), 2),
                benefit = Math.Round(group.Average(x => x.Benefit), 2)
            })
            .ToArray();

        return Results.Ok(new { feedback, summary });
    }

    private static async Task<IResult> UpsertExerciseFeedbackAsync(Guid organizationId, Guid sessionId, Guid exerciseId, Guid memberId, UpdateFootballExerciseFeedbackRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        if (membership.Membership!.MemberId != memberId) return Results.Forbid();
        if (request.Fun is < 1 or > 5 || request.Difficulty is < 1 or > 5 || request.Benefit is < 1 or > 5)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["feedback"] = ["Spaß, Schwierigkeit und Nutzen müssen zwischen 1 und 5 liegen."] });

        var sessionExists = await db.FootballSessions.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct);
        if (!sessionExists) return Results.NotFound();
        var exerciseInSession = await db.FootballTrainingBlocks.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.ExerciseId == exerciseId, ct);
        if (!exerciseInSession) return Results.ValidationProblem(new Dictionary<string, string[]> { ["exercise"] = ["Die Übung ist nicht Teil dieses Trainingsplans."] });

        var entity = await db.FootballExerciseFeedback.SingleOrDefaultAsync(x =>
            x.OrganizationId == organizationId && x.SessionId == sessionId && x.ExerciseId == exerciseId && x.MemberId == memberId, ct);
        var now = clock.GetUtcNow();
        if (entity is null)
        {
            entity = new FootballExerciseFeedback
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId, ExerciseId = exerciseId, MemberId = memberId, CreatedAt = now
            };
            db.FootballExerciseFeedback.Add(entity);
        }
        entity.Fun = request.Fun;
        entity.Difficulty = request.Difficulty;
        entity.Benefit = request.Benefit;
        entity.Comment = Clean(request.Comment, 1000);
        entity.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return Results.Ok(entity);
    }

    private static async Task<IResult> GetMemberTrainingHistoryAsync(Guid organizationId, Guid memberId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, int? take, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership.Result;
        if (!await access.IsActiveMemberAsync(organizationId, memberId, ct)) return Results.NotFound();

        var count = Math.Clamp(take ?? 20, 1, 50);
        var sessionIds = await db.FootballAttendances.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MemberId == memberId && x.Status == FootballAttendanceStatus.Accepted)
            .Select(x => x.SessionId)
            .ToArrayAsync(ct);

        var sessions = await db.FootballSessions.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && sessionIds.Contains(x.Id) && x.StartsAt < clock.GetUtcNow() && !x.IsCancelled)
            .OrderByDescending(x => x.StartsAt)
            .Take(count)
            .ToArrayAsync(ct);

        var selectedIds = sessions.Select(x => x.Id).ToArray();
        var loads = await db.FootballSessionLoads.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.MemberId == memberId && selectedIds.Contains(x.SessionId))
            .ToDictionaryAsync(x => x.SessionId, ct);
        var plannedMinutes = await db.FootballTrainingBlocks.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && selectedIds.Contains(x.SessionId))
            .GroupBy(x => x.SessionId)
            .Select(x => new { SessionId = x.Key, Minutes = x.Sum(block => block.DurationMinutes) })
            .ToDictionaryAsync(x => x.SessionId, x => x.Minutes, ct);

        var history = sessions.Select(session =>
        {
            loads.TryGetValue(session.Id, out var load);
            var minutes = load?.MinutesCompleted ?? session.DurationMinutes;
            return new
            {
                session,
                load,
                plannedMinutes = plannedMinutes.GetValueOrDefault(session.Id, session.DurationMinutes),
                trainingLoad = load is null ? (int?)null : load.Rpe * minutes
            };
        });

        return Results.Ok(history);
    }

    private static async Task<(OrganizationMembership? Membership, IResult? Result)> RequireMembershipAsync(Guid organizationId, ClaimsPrincipal principal, IOrganizationAccessService access, CancellationToken ct)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId)) return (null, Results.Unauthorized());
        var membership = await access.GetActiveMembershipAsync(organizationId, userId, ct);
        return membership is null ? (null, Results.Forbid()) : (membership, null);
    }

    private static async Task<(OrganizationMembership? Membership, IResult? Result)> RequireCoachAsync(Guid organizationId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership.Result is not null) return membership;
        return await CanCoachAsync(organizationId, membership.Membership!, db, ct) ? membership : (membership.Membership, Results.Forbid());
    }

    private static async Task<bool> CanCoachAsync(Guid organizationId, OrganizationMembership membership, IFootballDbContext db, CancellationToken ct)
    {
        if (membership.PermissionRole >= PermissionRole.Moderator) return true;
        return await db.FootballMemberProfiles.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == organizationId && x.MemberId == membership.MemberId && x.TeamRole == FootballTeamRole.Coach, ct);
    }

    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string[] CleanArray(IEnumerable<string>? values, int maxItems, int maxLength) => values?
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x.Trim()[..Math.Min(x.Trim().Length, maxLength)])
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(maxItems)
        .ToArray() ?? [];
}

public sealed record UpsertFootballProfileRequest(FootballTeamRole TeamRole, FootballPosition? Position, int? ShirtNumber, string? Description, string[]? Strengths, string[]? DevelopmentAreas, string[]? SecondaryPositions);
public sealed record UpdateFootballAvailabilityRequest(FootballAvailabilityStatus Status, int MaxLoadPercent, string? Note);
public sealed record CreateFootballExerciseRequest(string Title, string? Description, FootballExerciseCategory Category, FootballExerciseLocation Location, FootballIntensity Intensity, int MinPlayers, int? MaxPlayers, int DefaultDurationMinutes, string? Focus, string[]? Equipment, string[]? Tags);
public sealed record CreateFootballSessionRequest(FootballSessionKind Kind, string Title, string? Focus, string? Location, string? Opponent, DateTimeOffset StartsAt, int DurationMinutes);
public sealed record UpdateFootballAttendanceRequest(FootballAttendanceStatus Status, string? Note);
public sealed record UpdateFootballSessionLoadRequest(int Rpe, int? MinutesCompleted, string? Note);
public sealed record ReplaceFootballTrainingBlocksRequest(IReadOnlyList<FootballTrainingBlockRequest> Blocks);
public sealed record FootballTrainingBlockRequest(Guid? ExerciseId, string Title, string? Description, string? CoachingPoints, int DurationMinutes, Guid? ResponsibleMemberId, string? AiReason);
public sealed record UpdateFootballExerciseFeedbackRequest(int Fun, int Difficulty, int Benefit, string? Comment);
