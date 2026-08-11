using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Football.Domain;
using CommunityIntranet.Modules.Football.Persistence;
using CommunityIntranet.Modules.Football.Planning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Football.Endpoints;

public static class FootballPlanningEndpoints
{
    public static IEndpointRouteBuilder MapFootballPlanningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations/{organizationId:guid}/football")
            .WithTags("Football")
            .RequireAuthorization();

        group.MapPost("/sessions/{sessionId:guid}/plan/suggest", SuggestPlanAsync);
        return endpoints;
    }

    private static async Task<IResult> SuggestPlanAsync(
        Guid organizationId,
        Guid sessionId,
        SuggestFootballTrainingPlanRequest request,
        ClaimsPrincipal principal,
        IFootballDbContext db,
        IOrganizationAccessService access,
        IFootballTrainingPlanner planner,
        CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership is null) return Results.Forbid();

        if (!await CanCoachAsync(organizationId, membership, db, ct))
            return Results.Forbid();

        if (request.ExpectedPlayerCount is < 1 or > 60)
        {
            return Results.BadRequest(new
            {
                message = "ExpectedPlayerCount muss zwischen 1 und 60 liegen."
            });
        }

        var suggestion = await planner.SuggestAsync(
            organizationId,
            sessionId,
            ct,
            request.ExpectedPlayerCount);
        return suggestion is null ? Results.NotFound() : Results.Ok(suggestion);
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

    private static async Task<bool> CanCoachAsync(
        Guid organizationId,
        OrganizationMembership membership,
        IFootballDbContext db,
        CancellationToken ct)
    {
        if (membership.PermissionRole >= PermissionRole.Moderator) return true;
        return await db.FootballMemberProfiles.AsNoTracking().AnyAsync(
            x => x.OrganizationId == organizationId
                 && x.MemberId == membership.MemberId
                 && x.TeamRole == FootballTeamRole.Coach,
            ct);
    }
}

public sealed record SuggestFootballTrainingPlanRequest(int? ExpectedPlayerCount);