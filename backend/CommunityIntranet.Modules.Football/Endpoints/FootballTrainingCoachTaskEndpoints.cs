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

public static class FootballTrainingCoachTaskEndpoints
{
    public static IEndpointRouteBuilder MapFootballTrainingCoachTaskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations/{organizationId:guid}/football/sessions/{sessionId:guid}/blocks")
            .WithTags("Football")
            .RequireAuthorization();

        group.MapGet("/coach-tasks", ListAsync);
        group.MapPut("/{trainingBlockId:guid}/coach-tasks", ReplaceAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid organizationId,
        Guid sessionId,
        ClaimsPrincipal principal,
        IFootballDbContext db,
        IOrganizationAccessService access,
        CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership is null) return Results.Forbid();

        var sessionExists = await db.FootballSessions.AsNoTracking()
            .AnyAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct);
        if (!sessionExists) return Results.NotFound();

        var tasks = await db.FootballTrainingCoachTasks.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId)
            .OrderBy(x => x.TrainingBlockId)
            .ThenBy(x => x.SortOrder)
            .ToArrayAsync(ct);
        return Results.Ok(tasks);
    }

    private static async Task<IResult> ReplaceAsync(
        Guid organizationId,
        Guid sessionId,
        Guid trainingBlockId,
        ReplaceFootballTrainingCoachTasksRequest request,
        ClaimsPrincipal principal,
        IFootballDbContext db,
        IOrganizationAccessService access,
        TimeProvider clock,
        CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();

        if (request.Tasks.Count > 12
            || request.Tasks.Any(x => x.MemberId == Guid.Empty
                || string.IsNullOrWhiteSpace(x.Role)
                || string.IsNullOrWhiteSpace(x.Task)))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["tasks"] = ["Maximal 12 Trainer-Aufgaben; Trainer, Rolle und Aufgabe müssen gesetzt sein."]
            });
        }

        var blockExists = await db.FootballTrainingBlocks.AsNoTracking().AnyAsync(x =>
            x.OrganizationId == organizationId && x.SessionId == sessionId && x.Id == trainingBlockId, ct);
        if (!blockExists) return Results.NotFound();

        var requestedMemberIds = request.Tasks.Select(x => x.MemberId).Distinct().ToArray();
        var validCoachIds = await db.FootballMemberProfiles.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId
                && requestedMemberIds.Contains(x.MemberId)
                && (x.TeamRole == FootballTeamRole.Coach || x.TeamRole == FootballTeamRole.Staff))
            .Select(x => x.MemberId)
            .ToArrayAsync(ct);
        if (requestedMemberIds.Except(validCoachIds).Any())
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["memberId"] = ["Aufgaben können nur Trainern oder Staff-Mitgliedern zugewiesen werden."]
            });
        }

        var existing = await db.FootballTrainingCoachTasks
            .Where(x => x.OrganizationId == organizationId
                && x.SessionId == sessionId
                && x.TrainingBlockId == trainingBlockId)
            .ToArrayAsync(ct);
        db.FootballTrainingCoachTasks.RemoveRange(existing);

        var now = clock.GetUtcNow();
        var tasks = request.Tasks.Select((x, index) => new FootballTrainingCoachTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SessionId = sessionId,
            TrainingBlockId = trainingBlockId,
            MemberId = x.MemberId,
            Role = Clean(x.Role, 120),
            Task = Clean(x.Task, 1000),
            SortOrder = index,
            UpdatedAt = now,
            UpdatedByMemberId = membership.MemberId
        }).ToArray();
        db.FootballTrainingCoachTasks.AddRange(tasks);
        await db.SaveChangesAsync(ct);
        return Results.Ok(tasks);
    }

    private static string Clean(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, maxLength)];
    }

    private static async Task<OrganizationMembership?> RequireCoachAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IFootballDbContext db,
        IOrganizationAccessService access,
        CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership is null) return null;
        if (membership.PermissionRole >= PermissionRole.Moderator) return membership;
        var coach = await db.FootballMemberProfiles.AsNoTracking().AnyAsync(x =>
            x.OrganizationId == organizationId
            && x.MemberId == membership.MemberId
            && x.TeamRole == FootballTeamRole.Coach, ct);
        return coach ? membership : null;
    }

    private static async Task<OrganizationMembership?> RequireMembershipAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService access,
        CancellationToken ct)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId)) return null;
        return await access.GetActiveMembershipAsync(organizationId, userId, ct);
    }
}

public sealed record ReplaceFootballTrainingCoachTasksRequest(IReadOnlyList<FootballTrainingCoachTaskRequest> Tasks);
public sealed record FootballTrainingCoachTaskRequest(Guid MemberId, string Role, string Task);
