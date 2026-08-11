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

public static class FootballTrainingBriefingEndpoints
{
    public static IEndpointRouteBuilder MapFootballTrainingBriefingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/organizations/{organizationId:guid}/football/sessions/{sessionId:guid}/blocks")
            .WithTags("Football")
            .RequireAuthorization();

        group.MapPut("/{trainingBlockId:guid}/briefing", UpdateAsync);
        return endpoints;
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid sessionId,
        Guid trainingBlockId,
        UpdateFootballTrainingBriefingRequest request,
        ClaimsPrincipal principal,
        IFootballDbContext db,
        IOrganizationAccessService access,
        CancellationToken ct)
    {
        var membership = await RequireCoachAsync(organizationId, principal, db, access, ct);
        if (membership is null) return Results.Forbid();

        var block = await db.FootballTrainingBlocks.SingleOrDefaultAsync(x =>
            x.OrganizationId == organizationId && x.SessionId == sessionId && x.Id == trainingBlockId, ct);
        if (block is null) return Results.NotFound();

        block.Description = Clean(request.SetupAndFlow, 2000);
        block.CoachingPoints = Clean(request.CoachingPoints, 2000);
        await db.SaveChangesAsync(ct);
        return Results.Ok(block);
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
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
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var userId)) return null;
        var membership = await access.GetActiveMembershipAsync(organizationId, userId, ct);
        if (membership is null) return null;
        if (membership.PermissionRole >= PermissionRole.Moderator) return membership;
        var coach = await db.FootballMemberProfiles.AsNoTracking().AnyAsync(x =>
            x.OrganizationId == organizationId
            && x.MemberId == membership.MemberId
            && x.TeamRole == FootballTeamRole.Coach, ct);
        return coach ? membership : null;
    }
}

public sealed record UpdateFootballTrainingBriefingRequest(string? SetupAndFlow, string? CoachingPoints);
