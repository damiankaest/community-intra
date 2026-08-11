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

public static class FootballTrainingPlanningEndpoints
{
    public static IEndpointRouteBuilder MapFootballTrainingPlanningEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
                "/api/organizations/{organizationId:guid}/football/sessions/{sessionId:guid}/plan/suggest",
                SuggestPlanAsync)
            .WithTags("Football")
            .RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> SuggestPlanAsync(
        Guid organizationId,
        Guid sessionId,
        ClaimsPrincipal principal,
        IFootballDbContext db,
        IOrganizationAccessService access,
        IFootballTrainingPlanner planner,
        CancellationToken ct)
    {
        var membership = await RequireMembershipAsync(organizationId, principal, access, ct);
        if (membership is null) return Results.Forbid();

        var canCoach = membership.PermissionRole >= PermissionRole.Moderator
            || await db.FootballMemberProfiles.AsNoTracking().AnyAsync(
                x => x.OrganizationId == organizationId
                    && x.MemberId == membership.MemberId
                    && x.TeamRole == FootballTeamRole.Coach,
                ct);
        if (!canCoach) return Results.Forbid();

        var suggestion = await planner.SuggestAsync(organizationId, sessionId, ct);
        return suggestion is null ? Results.NotFound() : Results.Ok(suggestion);
    }

    private static async Task<OrganizationMembership?> RequireMembershipAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService access,
        CancellationToken ct)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId)) return null;
        return await access.GetActiveMembershipAsync(organizationId, userId, ct);
    }
}
