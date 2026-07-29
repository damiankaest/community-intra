using System.Security.Claims;
using System.Text.Json;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.ActivityFeed.Contracts;
using CommunityIntranet.Modules.ActivityFeed.Domain;
using CommunityIntranet.Modules.ActivityFeed.Persistence;
using CommunityIntranet.Modules.Awards.Persistence;
using CommunityIntranet.Modules.Incidents.Domain;
using CommunityIntranet.Modules.Incidents.Persistence;
using CommunityIntranet.Modules.Members.Persistence;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.Projects.Domain;
using CommunityIntranet.Modules.Projects.Persistence;
using CommunityIntranet.Modules.Tasks.Domain;
using CommunityIntranet.Modules.Tasks.Persistence;
using CommunityIntranet.Modules.ThemePacks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.ActivityFeed.Endpoints;

public static class ActivityEndpoints
{
    public static IEndpointRouteBuilder MapActivityFeedEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}")
            .WithTags("Activity Feed")
            .RequireAuthorization();
        group.MapGet("/activities", ListAsync);
        group.MapGet("/dashboard", GetDashboardAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid organizationId,
        int? limit,
        ClaimsPrincipal principal,
        IActivityDbContext dbContext,
        IOrganizationAccessService accessService,
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

        var activities = await dbContext.Activities
            .AsNoTracking()
            .Where(activity => activity.OrganizationId == organizationId)
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(Math.Clamp(limit ?? 50, 1, 100))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(await ToResponsesAsync(
            activities,
            accessService,
            cancellationToken));
    }

    private static async Task<IResult> GetDashboardAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IActivityDbContext activityDbContext,
        IAwardDbContext awardDbContext,
        IIncidentDbContext incidentDbContext,
        IMemberDbContext memberDbContext,
        IOrganizationDbContext organizationDbContext,
        IProjectDbContext projectDbContext,
        ITaskDbContext taskDbContext,
        IOrganizationAccessService accessService,
        IThemePackCatalog themePackCatalog,
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

        var memberCount = await memberDbContext.OrganizationMembers.CountAsync(
            member =>
                member.OrganizationId == organizationId && member.IsActive,
            cancellationToken);
        var openTaskCount = await taskDbContext.WorkTasks.CountAsync(
            task =>
                task.OrganizationId == organizationId
                && task.Status != WorkTaskStatus.Done
                && task.Status != WorkTaskStatus.Cancelled,
            cancellationToken);
        var projectCount = await projectDbContext.Projects.CountAsync(
            project =>
                project.OrganizationId == organizationId
                && project.Status != ProjectStatus.Completed
                && project.Status != ProjectStatus.Cancelled,
            cancellationToken);
        var incidentCount = await incidentDbContext.Incidents.CountAsync(
            incident =>
                incident.OrganizationId == organizationId
                && incident.Status != IncidentStatus.Resolved
                && incident.Status != IncidentStatus.Rejected,
            cancellationToken);

        var currentAward = await awardDbContext.Awards
            .AsNoTracking()
            .Where(award =>
                award.OrganizationId == organizationId && award.IsPublic)
            .OrderByDescending(award => award.AwardedAt)
            .FirstOrDefaultAsync(cancellationToken);
        CurrentAwardResponse? currentAwardResponse = null;
        if (currentAward is not null)
        {
            var targetName = await accessService.GetMemberDisplayNameAsync(
                organizationId,
                currentAward.AwardedToMemberId,
                cancellationToken);
            currentAwardResponse = new CurrentAwardResponse(
                currentAward.Id,
                currentAward.Name,
                currentAward.Description,
                targetName,
                currentAward.AwardedAt,
                currentAward.Icon,
                currentAward.Category);
        }

        var recent = await activityDbContext.Activities
            .AsNoTracking()
            .Where(activity => activity.OrganizationId == organizationId)
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(10)
            .ToArrayAsync(cancellationToken);
        var activities = await ToResponsesAsync(
            recent,
            accessService,
            cancellationToken);
        var systemMessage = await GetSystemMessageAsync(
            organizationId,
            organizationDbContext,
            themePackCatalog,
            cancellationToken);

        return Results.Ok(new DashboardResponse(
            memberCount,
            openTaskCount,
            projectCount,
            incidentCount,
            currentAwardResponse,
            activities,
            systemMessage));
    }

    private static async Task<string> GetSystemMessageAsync(
        Guid organizationId,
        IOrganizationDbContext organizationDbContext,
        IThemePackCatalog themePackCatalog,
        CancellationToken cancellationToken)
    {
        var themePackId = await organizationDbContext.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => organization.ThemePackId)
            .SingleOrDefaultAsync(cancellationToken);
        if (themePackId is null)
        {
            return "Alle Systeme melden Betriebsbereitschaft.";
        }

        var themePack = await themePackCatalog.FindByIdAsync(
            themePackId.Value,
            cancellationToken);
        var messages = themePack?.Configuration.StatusMessages;
        if (messages is null || messages.Count == 0)
        {
            return "Alle Systeme melden Betriebsbereitschaft.";
        }

        var index = DateTimeOffset.UtcNow.DayOfYear % messages.Count;
        return messages[index];
    }

    private static async Task<IReadOnlyList<ActivityResponse>> ToResponsesAsync(
        IReadOnlyCollection<ActivityEntry> activities,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var result = new List<ActivityResponse>(activities.Count);
        foreach (var activity in activities)
        {
            var actorName = await accessService.GetMemberDisplayNameAsync(
                activity.OrganizationId,
                activity.ActorMemberId,
                cancellationToken);
            using var document = JsonDocument.Parse(activity.DataJson);
            result.Add(new ActivityResponse(
                activity.Id,
                activity.ActivityType,
                activity.ActorMemberId,
                actorName,
                activity.EntityType,
                activity.EntityId,
                document.RootElement.Clone(),
                activity.EventVersion,
                activity.CreatedAt));
        }

        return result;
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
