using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Awards.Contracts;
using CommunityIntranet.Modules.Awards.Domain;
using CommunityIntranet.Modules.Awards.Persistence;
using CommunityIntranet.Modules.ThemePacks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Awards.Endpoints;

public static class AwardEndpoints
{
    public static IEndpointRouteBuilder MapAwardEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/awards")
            .WithTags("Awards")
            .RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapGet("/templates", ListTemplatesAsync);
        group.MapPost("/", GrantAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid organizationId,
        Guid? memberId,
        ClaimsPrincipal principal,
        IAwardDbContext dbContext,
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

        var query = dbContext.Awards
            .AsNoTracking()
            .Where(award => award.OrganizationId == organizationId);
        if (memberId is not null)
        {
            query = query.Where(award =>
                award.AwardedToMemberId == memberId);
        }

        var awards = await query
            .OrderByDescending(award => award.AwardedAt)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(awards.Select(ToResponse));
    }

    private static async Task<IResult> ListTemplatesAsync(
        Guid organizationId,
        string themePackKey,
        ClaimsPrincipal principal,
        IThemePackCatalog themePackCatalog,
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

        var themePack = await themePackCatalog.FindByKeyAsync(
            themePackKey,
            cancellationToken);
        return themePack is null
            ? Results.NotFound()
            : Results.Ok(themePack.Configuration.AwardTemplates.Select(
                template => new AwardTemplateResponse(
                    template.Name,
                    template.DescriptionTemplate)));
    }

    private static async Task<IResult> GrantAsync(
        Guid organizationId,
        GrantAwardRequest request,
        ClaimsPrincipal principal,
        IAwardDbContext dbContext,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
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

        if (!access.Membership!.PermissionRole.CanGrantAwards())
        {
            return Results.Forbid();
        }

        var validation = Validate(request);
        if (validation is not null)
        {
            return validation;
        }

        var targetDisplayName =
            await accessService.GetMemberDisplayNameAsync(
                organizationId,
                request.AwardedToMemberId,
                cancellationToken);
        if (targetDisplayName is null)
        {
            return Validation(
                "AwardedToMemberId",
                "The selected member does not exist.");
        }

        var award = new Award
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            AwardedToMemberId = request.AwardedToMemberId,
            AwardedByMemberId = access.Membership.MemberId,
            AwardedAt = timeProvider.GetUtcNow(),
            Icon = request.Icon.Trim(),
            Category = request.Category.Trim(),
            IsPublic = request.IsPublic
        };
        dbContext.Awards.Add(award);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "award.granted",
            access.Membership.MemberId,
            "award",
            award.Id,
            new Dictionary<string, string?>
            {
                ["awardName"] = award.Name,
                ["targetMemberName"] = targetDisplayName
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/awards/{award.Id}",
            ToResponse(award));
    }

    private static IResult? Validate(GrantAwardRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 160)
        {
            return Validation(
                "Name",
                "Name must contain 1 to 160 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Description)
            || request.Description.Trim().Length > 2000)
        {
            return Validation(
                "Description",
                "Description must contain 1 to 2000 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Icon)
            || request.Icon.Trim().Length > 50)
        {
            return Validation("Icon", "Icon must contain 1 to 50 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Category)
            || request.Category.Trim().Length > 100)
        {
            return Validation(
                "Category",
                "Category must contain 1 to 100 characters.");
        }

        return null;
    }

    private static AwardResponse ToResponse(Award award) =>
        new(
            award.Id,
            award.Name,
            award.Description,
            award.AwardedToMemberId,
            award.AwardedByMemberId,
            award.AwardedAt,
            award.Icon,
            award.Category,
            award.IsPublic);

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { [key] = [message] });

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
