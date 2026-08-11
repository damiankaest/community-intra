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
        group.MapGet("/exercises", ListExercisesAsync);
        group.MapPost("/exercises", CreateExerciseAsync);
        group.MapGet("/sessions", ListSessionsAsync);
        group.MapPost("/sessions", CreateSessionAsync);
        group.MapGet("/sessions/{sessionId:guid}", GetSessionAsync);
        group.MapPut("/sessions/{sessionId:guid}/attendance/{memberId:guid}", UpdateAttendanceAsync);
        group.MapPut("/sessions/{sessionId:guid}/blocks", ReplaceTrainingBlocksAsync);
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
        if (!Enum.IsDefined(request.TeamRole) || (request.Position is not null && !Enum.IsDefined(request.Position.Value))) return Results.ValidationProblem(new Dictionary<string, string[]> { ["profile"] = ["Ungültige Fußballrolle oder Position."] });

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
            Id = Guid.NewGuid(), OrganizationId = organizationId, Title = request.Title.Trim(), Description = Clean(request.Description, 3000) ?? string.Empty,
            Category = request.Category, Location = request.Location, Intensity = request.Intensity, MinPlayers = request.MinPlayers, MaxPlayers = request.MaxPlayers,
            DefaultDurationMinutes = request.DefaultDurationMinutes, Focus = Clean(request.Focus, 1000) ?? string.Empty,
            Equipment = CleanArray(request.Equipment, 20, 80), Tags = CleanArray(request.Tags, 20, 80), CreatedByMemberId = membership.Membership!.MemberId,
            CreatedAt = now, UpdatedAt = now
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
        if (string.IsNullOrWhiteSpace(request.Title) || request.DurationMinutes is < 5 or > 480) return Results.ValidationProblem(new Dictionary<string, string[]> { ["session"] = ["Titel oder Dauer ist ungültig."] });
        var now = clock.GetUtcNow();
        var session = new FootballSession { Id = Guid.NewGuid(), OrganizationId = organizationId, Kind = request.Kind, Title = request.Title.Trim(), Focus = Clean(request.Focus, 1000), Location = Clean(request.Location, 300), Opponent = Clean(request.Opponent, 180), StartsAt = request.StartsAt, DurationMinutes = request.DurationMinutes, CreatedByMemberId = membership.Membership!.MemberId, CreatedAt = now, UpdatedAt = now };
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
        return Results.Ok(new { session, attendance, blocks });
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
        if (entity is null) { entity = new FootballAttendance { Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId, MemberId = memberId }; db.FootballAttendances.Add(entity); }
        entity.Status = request.Status; entity.Note = Clean(request.Note, 500); entity.UpdatedAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return Results.Ok(entity);
    }

    private static async Task<IResult> ReplaceTrainingBlocksAsync(Guid organizationId, Guid sessionId, ReplaceFootballTrainingBlocksRequest request, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership.Result is not null) return membership.Result;
        var session = await db.FootballSessions.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct);
        if (session is null) return Results.NotFound();
        if (request.Blocks.Count > 30 || request.Blocks.Any(x => string.IsNullOrWhiteSpace(x.Title) || x.DurationMinutes is < 1 or > 180)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["blocks"] = ["Trainingsblöcke sind ungültig."] });

        var existing = await db.FootballTrainingBlocks.Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).ToArrayAsync(ct);
        db.FootballTrainingBlocks.RemoveRange(existing);
        var blocks = request.Blocks.Select((x, index) => new FootballTrainingBlock { Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId, ExerciseId = x.ExerciseId, Title = x.Title.Trim(), Description = Clean(x.Description, 2000), CoachingPoints = Clean(x.CoachingPoints, 2000), SortOrder = index, DurationMinutes = x.DurationMinutes, ResponsibleMemberId = x.ResponsibleMemberId, AiReason = Clean(x.AiReason, 1500) }).ToArray();
        db.FootballTrainingBlocks.AddRange(blocks);
        await db.SaveChangesAsync(ct);
        return Results.Ok(blocks);
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
        return await db.FootballMemberProfiles.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.MemberId == membership.MemberId && x.TeamRole == FootballTeamRole.Coach, ct);
    }

    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private static string[] CleanArray(IEnumerable<string>? values, int maxItems, int maxLength) => values?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()[..Math.Min(x.Trim().Length, maxLength)]).Distinct(StringComparer.OrdinalIgnoreCase).Take(maxItems).ToArray() ?? [];
}

public sealed record UpsertFootballProfileRequest(FootballTeamRole TeamRole, FootballPosition? Position, int? ShirtNumber, string? Description, string[]? Strengths, string[]? DevelopmentAreas, string[]? SecondaryPositions);
public sealed record CreateFootballExerciseRequest(string Title, string? Description, FootballExerciseCategory Category, FootballExerciseLocation Location, FootballIntensity Intensity, int MinPlayers, int? MaxPlayers, int DefaultDurationMinutes, string? Focus, string[]? Equipment, string[]? Tags);
public sealed record CreateFootballSessionRequest(FootballSessionKind Kind, string Title, string? Focus, string? Location, string? Opponent, DateTimeOffset StartsAt, int DurationMinutes);
public sealed record UpdateFootballAttendanceRequest(FootballAttendanceStatus Status, string? Note);
public sealed record ReplaceFootballTrainingBlocksRequest(IReadOnlyList<FootballTrainingBlockRequest> Blocks);
public sealed record FootballTrainingBlockRequest(Guid? ExerciseId, string Title, string? Description, string? CoachingPoints, int DurationMinutes, Guid? ResponsibleMemberId, string? AiReason);
