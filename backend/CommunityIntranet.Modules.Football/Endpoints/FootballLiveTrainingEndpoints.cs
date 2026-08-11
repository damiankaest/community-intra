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

public static class FootballLiveTrainingEndpoints
{
    public static IEndpointRouteBuilder MapFootballLiveTrainingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations/{organizationId:guid}/football/sessions/{sessionId:guid}/live")
            .WithTags("Football")
            .RequireAuthorization();

        group.MapGet("/", GetAsync);
        group.MapPost("/start", StartAsync);
        group.MapPost("/pause", PauseAsync);
        group.MapPost("/resume", ResumeAsync);
        group.MapPost("/complete", CompleteAsync);
        group.MapPost("/blocks/{trainingBlockId:guid}/activate", ActivateBlockAsync);
        group.MapPost("/blocks/{trainingBlockId:guid}/pause", PauseBlockAsync);
        group.MapPost("/blocks/{trainingBlockId:guid}/resume", ResumeBlockAsync);
        group.MapPost("/blocks/{trainingBlockId:guid}/reset", ResetBlockAsync);
        group.MapPost("/blocks/{trainingBlockId:guid}/complete", CompleteBlockAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(Guid organizationId, Guid sessionId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership is null) return Results.Forbid();
        var sessionExists = await db.FootballSessions.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.Id == sessionId, ct);
        if (!sessionExists) return Results.NotFound();
        return Results.Ok(await BuildStateAsync(organizationId, sessionId, db, clock, ct));
    }

    private static async Task<IResult> StartAsync(Guid organizationId, Guid sessionId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();
        var session = await db.FootballSessions.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == sessionId && x.Kind == FootballSessionKind.Training && !x.IsCancelled, ct);
        if (session is null) return Results.NotFound();
        var blocks = await db.FootballTrainingBlocks.Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).OrderBy(x => x.SortOrder).ToArrayAsync(ct);
        if (blocks.Length == 0) return Results.BadRequest(new { message = "Das Training hat noch keine Trainingsblöcke." });

        var now = clock.GetUtcNow();
        var run = await db.FootballLiveTrainingRuns.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId, ct);
        if (run is null)
        {
            run = new FootballLiveTrainingRun
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId,
                Status = FootballLiveTrainingStatus.Running, ActiveTrainingBlockId = blocks[0].Id,
                StartedAt = now, UpdatedAt = now, UpdatedByMemberId = membership.MemberId
            };
            db.FootballLiveTrainingRuns.Add(run);
        }
        else if (run.Status == FootballLiveTrainingStatus.NotStarted)
        {
            run.Status = FootballLiveTrainingStatus.Running;
            run.StartedAt = now;
            run.ActiveTrainingBlockId = blocks[0].Id;
            run.UpdatedAt = now;
            run.UpdatedByMemberId = membership.MemberId;
        }

        await EnsureBlockRunStartedAsync(organizationId, sessionId, blocks[0].Id, db, now, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(await BuildStateAsync(organizationId, sessionId, db, clock, ct));
    }

    private static async Task<IResult> PauseAsync(Guid organizationId, Guid sessionId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();
        var run = await GetRunAsync(organizationId, sessionId, db, ct);
        if (run is null) return Results.NotFound();
        if (run.Status == FootballLiveTrainingStatus.Running)
        {
            var now = clock.GetUtcNow();
            run.Status = FootballLiveTrainingStatus.Paused;
            run.PausedAt = now;
            run.UpdatedAt = now;
            run.UpdatedByMemberId = membership.MemberId;
            if (run.ActiveTrainingBlockId is Guid blockId) await PauseBlockInternalAsync(organizationId, sessionId, blockId, db, now, ct);
            await db.SaveChangesAsync(ct);
        }
        return Results.Ok(await BuildStateAsync(organizationId, sessionId, db, clock, ct));
    }

    private static async Task<IResult> ResumeAsync(Guid organizationId, Guid sessionId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();
        var run = await GetRunAsync(organizationId, sessionId, db, ct);
        if (run is null) return Results.NotFound();
        if (run.Status == FootballLiveTrainingStatus.Paused)
        {
            var now = clock.GetUtcNow();
            if (run.PausedAt is { } pausedAt) run.AccumulatedPausedSeconds += Math.Max(0, (int)(now - pausedAt).TotalSeconds);
            run.Status = FootballLiveTrainingStatus.Running;
            run.PausedAt = null;
            run.UpdatedAt = now;
            run.UpdatedByMemberId = membership.MemberId;
            if (run.ActiveTrainingBlockId is Guid blockId) await ResumeBlockInternalAsync(organizationId, sessionId, blockId, db, now, ct);
            await db.SaveChangesAsync(ct);
        }
        return Results.Ok(await BuildStateAsync(organizationId, sessionId, db, clock, ct));
    }

    private static async Task<IResult> ActivateBlockAsync(Guid organizationId, Guid sessionId, Guid trainingBlockId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();
        var run = await GetRunAsync(organizationId, sessionId, db, ct);
        if (run is null || run.Status == FootballLiveTrainingStatus.Completed) return Results.NotFound();
        var blockExists = await db.FootballTrainingBlocks.AnyAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.Id == trainingBlockId, ct);
        if (!blockExists) return Results.NotFound();
        var now = clock.GetUtcNow();
        if (run.ActiveTrainingBlockId is Guid oldId && oldId != trainingBlockId) await PauseBlockInternalAsync(organizationId, sessionId, oldId, db, now, ct);
        run.ActiveTrainingBlockId = trainingBlockId;
        run.UpdatedAt = now;
        run.UpdatedByMemberId = membership.MemberId;
        if (run.Status == FootballLiveTrainingStatus.Running) await ResumeBlockInternalAsync(organizationId, sessionId, trainingBlockId, db, now, ct);
        await db.SaveChangesAsync(ct);
        return Results.Ok(await BuildStateAsync(organizationId, sessionId, db, clock, ct));
    }

    private static Task<IResult> PauseBlockAsync(Guid organizationId, Guid sessionId, Guid trainingBlockId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct) => ChangeBlockAsync(organizationId, sessionId, trainingBlockId, principal, db, access, clock, "pause", ct);
    private static Task<IResult> ResumeBlockAsync(Guid organizationId, Guid sessionId, Guid trainingBlockId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct) => ChangeBlockAsync(organizationId, sessionId, trainingBlockId, principal, db, access, clock, "resume", ct);
    private static Task<IResult> ResetBlockAsync(Guid organizationId, Guid sessionId, Guid trainingBlockId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct) => ChangeBlockAsync(organizationId, sessionId, trainingBlockId, principal, db, access, clock, "reset", ct);
    private static Task<IResult> CompleteBlockAsync(Guid organizationId, Guid sessionId, Guid trainingBlockId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct) => ChangeBlockAsync(organizationId, sessionId, trainingBlockId, principal, db, access, clock, "complete", ct);

    private static async Task<IResult> ChangeBlockAsync(Guid organizationId, Guid sessionId, Guid trainingBlockId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, string action, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();
        var blockExists = await db.FootballTrainingBlocks.AnyAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.Id == trainingBlockId, ct);
        if (!blockExists) return Results.NotFound();
        var now = clock.GetUtcNow();
        var blockRun = await db.FootballLiveTrainingBlockRuns.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.TrainingBlockId == trainingBlockId, ct);
        blockRun ??= await CreateBlockRunAsync(organizationId, sessionId, trainingBlockId, db, now);
        switch (action)
        {
            case "pause": AccumulateBlock(blockRun, now); blockRun.PausedAt = now; break;
            case "resume": blockRun.StartedAt = now; blockRun.PausedAt = null; blockRun.IsCompleted = false; blockRun.CompletedAt = null; break;
            case "reset": blockRun.AccumulatedSeconds = 0; blockRun.StartedAt = now; blockRun.PausedAt = null; blockRun.IsCompleted = false; blockRun.CompletedAt = null; break;
            case "complete": AccumulateBlock(blockRun, now); blockRun.StartedAt = null; blockRun.PausedAt = null; blockRun.IsCompleted = true; blockRun.CompletedAt = now; break;
        }
        blockRun.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return Results.Ok(await BuildStateAsync(organizationId, sessionId, db, clock, ct));
    }

    private static async Task<IResult> CompleteAsync(Guid organizationId, Guid sessionId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, TimeProvider clock, CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();
        var run = await GetRunAsync(organizationId, sessionId, db, ct);
        if (run is null) return Results.NotFound();
        var now = clock.GetUtcNow();
        if (run.ActiveTrainingBlockId is Guid blockId) await PauseBlockInternalAsync(organizationId, sessionId, blockId, db, now, ct);
        run.Status = FootballLiveTrainingStatus.Completed;
        run.CompletedAt = now;
        run.PausedAt = null;
        run.UpdatedAt = now;
        run.UpdatedByMemberId = membership.MemberId;
        await db.SaveChangesAsync(ct);
        return Results.Ok(await BuildStateAsync(organizationId, sessionId, db, clock, ct));
    }

    private static async Task<object> BuildStateAsync(Guid organizationId, Guid sessionId, IFootballDbContext db, TimeProvider clock, CancellationToken ct)
    {
        var run = await db.FootballLiveTrainingRuns.AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId, ct);
        var blocks = await db.FootballTrainingBlocks.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).OrderBy(x => x.SortOrder).ToArrayAsync(ct);
        var blockRuns = await db.FootballLiveTrainingBlockRuns.AsNoTracking().Where(x => x.OrganizationId == organizationId && x.SessionId == sessionId).ToArrayAsync(ct);
        return new { serverNow = clock.GetUtcNow(), run, blocks, blockRuns };
    }

    private static Task<FootballLiveTrainingRun?> GetRunAsync(Guid organizationId, Guid sessionId, IFootballDbContext db, CancellationToken ct) =>
        db.FootballLiveTrainingRuns.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId, ct);

    private static async Task EnsureBlockRunStartedAsync(Guid organizationId, Guid sessionId, Guid blockId, IFootballDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var blockRun = await db.FootballLiveTrainingBlockRuns.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.TrainingBlockId == blockId, ct);
        if (blockRun is null) await CreateBlockRunAsync(organizationId, sessionId, blockId, db, now);
        else if (!blockRun.IsCompleted) { blockRun.StartedAt = now; blockRun.PausedAt = null; blockRun.UpdatedAt = now; }
    }

    private static async Task ResumeBlockInternalAsync(Guid organizationId, Guid sessionId, Guid blockId, IFootballDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var blockRun = await db.FootballLiveTrainingBlockRuns.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.TrainingBlockId == blockId, ct);
        blockRun ??= await CreateBlockRunAsync(organizationId, sessionId, blockId, db, now);
        if (!blockRun.IsCompleted) { blockRun.StartedAt = now; blockRun.PausedAt = null; blockRun.UpdatedAt = now; }
    }

    private static async Task PauseBlockInternalAsync(Guid organizationId, Guid sessionId, Guid blockId, IFootballDbContext db, DateTimeOffset now, CancellationToken ct)
    {
        var blockRun = await db.FootballLiveTrainingBlockRuns.SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.SessionId == sessionId && x.TrainingBlockId == blockId, ct);
        if (blockRun is null || blockRun.IsCompleted) return;
        AccumulateBlock(blockRun, now);
        blockRun.PausedAt = now;
        blockRun.UpdatedAt = now;
    }

    private static Task<FootballLiveTrainingBlockRun> CreateBlockRunAsync(Guid organizationId, Guid sessionId, Guid blockId, IFootballDbContext db, DateTimeOffset now)
    {
        var entity = new FootballLiveTrainingBlockRun { Id = Guid.NewGuid(), OrganizationId = organizationId, SessionId = sessionId, TrainingBlockId = blockId, StartedAt = now, UpdatedAt = now };
        db.FootballLiveTrainingBlockRuns.Add(entity);
        return Task.FromResult(entity);
    }

    private static void AccumulateBlock(FootballLiveTrainingBlockRun blockRun, DateTimeOffset now)
    {
        if (blockRun.StartedAt is { } startedAt) blockRun.AccumulatedSeconds += Math.Max(0, (int)(now - startedAt).TotalSeconds);
        blockRun.StartedAt = null;
    }

    private static async Task<OrganizationMembership?> RequireCoachAsync(Guid organizationId, ClaimsPrincipal principal, IFootballDbContext db, IOrganizationAccessService access, CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership is null) return null;
        if (membership.PermissionRole >= PermissionRole.Moderator) return membership;
        var coach = await db.FootballMemberProfiles.AsNoTracking().AnyAsync(x => x.OrganizationId == organizationId && x.MemberId == membership.MemberId && x.TeamRole == FootballTeamRole.Coach, ct);
        return coach ? membership : null;
    }

    private static async Task<OrganizationMembership?> RequireMembershipAsync(Guid organizationId, ClaimsPrincipal principal, IOrganizationAccessService access, CancellationToken ct)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId)) return null;
        return await access.GetActiveMembershipAsync(organizationId, userId, ct);
    }
}
