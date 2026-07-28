using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Organizations.Contracts;
using CommunityIntranet.Modules.Organizations.Domain;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.Organizations.Services;
using CommunityIntranet.Modules.ThemePacks.Contracts;
using CommunityIntranet.Modules.ThemePacks.Seeding;
using CommunityIntranet.Modules.ThemePacks.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Organizations.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations")
            .WithTags("Organizations")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{organizationId:guid}", GetAsync);
        group.MapPut("/{organizationId:guid}", UpdateAsync);
        group.MapDelete("/{organizationId:guid}", ArchiveAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        IOrganizationDbContext dbContext,
        IOrganizationAccessService accessService,
        IThemePackCatalog themePackCatalog,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var memberships = await accessService.GetActiveMembershipsAsync(
            userId,
            cancellationToken);
        var membershipByOrganization = memberships.ToDictionary(
            membership => membership.OrganizationId);
        var organizationIds = membershipByOrganization.Keys.ToArray();
        var organizations = await dbContext.Organizations
            .AsNoTracking()
            .Where(organization =>
                organizationIds.Contains(organization.Id)
                && !organization.IsArchived)
            .OrderBy(organization => organization.Name)
            .ToListAsync(cancellationToken);
        var themePacks = await themePackCatalog.ListAsync(cancellationToken);
        var fallbackTheme = FindFallbackTheme(themePacks);
        if (fallbackTheme is null)
        {
            return ThemePacksUnavailable();
        }

        var themePackById = themePacks.ToDictionary(themePack => themePack.Id);
        var response = organizations.Select(organization =>
        {
            var membership = membershipByOrganization[organization.Id];
            var themePack = ResolveTheme(
                organization.ThemePackId,
                themePackById,
                fallbackTheme);

            return new OrganizationSummaryResponse(
                organization.Id,
                organization.Name,
                organization.Slug,
                organization.Description,
                themePack.Key,
                themePack.Version,
                organization.Language,
                membership.PermissionRole,
                membership.VisibleTitle);
        });

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateAsync(
        CreateOrganizationRequest request,
        ClaimsPrincipal principal,
        IValidator<CreateOrganizationRequest> validator,
        IOrganizationDbContext dbContext,
        IOrganizationOwnerProvisioner ownerProvisioner,
        IThemePackCatalog themePackCatalog,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return Results.Unauthorized();
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(
                ToValidationDictionary(validation.Errors));
        }

        var themePackKey =
            NormalizeOptional(request.ThemePackKey)
            ?? ThemePackSeeds.GenericCorporateKey;
        var themePack = await themePackCatalog.FindByKeyAsync(
            themePackKey,
            cancellationToken);
        if (themePack is null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ThemePackKey"] = ["The selected theme pack does not exist."]
                });
        }

        var baseSlug = SlugGenerator.Create(request.Name);
        var slug = baseSlug;
        if (await dbContext.Organizations.AnyAsync(
                organization => organization.Slug == slug,
                cancellationToken))
        {
            slug = $"{baseSlug}-{Guid.NewGuid():N}"[
                ..Math.Min(baseSlug.Length + 9, 140)];
        }

        var now = timeProvider.GetUtcNow();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            Description = NormalizeOptional(request.Description),
            ThemePackId = themePack.Id,
            EnabledModules = OrganizationModuleKeys.Normalize(
                request.EnabledModules),
            Language = request.Language.Trim(),
            TimeZone = request.TimeZone.Trim(),
            OwnerUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            IsArchived = false
        };

        dbContext.Organizations.Add(organization);
        ownerProvisioner.AddOwner(
            organization.Id,
            userId,
            NormalizeOptional(request.VisibleTitle));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/organizations/{organization.Id}",
            ToResponse(
                organization,
                themePack,
                PermissionRole.Owner,
                NormalizeOptional(request.VisibleTitle)));
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationDbContext dbContext,
        IOrganizationAccessService accessService,
        IThemePackCatalog themePackCatalog,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var membership = accessResult.Membership!;
        var organization = await dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == organizationId && !item.IsArchived,
                cancellationToken);
        if (organization is null)
        {
            return Results.NotFound();
        }

        var themePack = await ResolveThemeAsync(
            organization.ThemePackId,
            themePackCatalog,
            cancellationToken);
        return themePack is null
            ? ThemePacksUnavailable()
            : Results.Ok(ToResponse(
                organization,
                themePack,
                membership.PermissionRole,
                membership.VisibleTitle));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationRequest request,
        ClaimsPrincipal principal,
        IValidator<UpdateOrganizationRequest> validator,
        IOrganizationDbContext dbContext,
        IOrganizationAccessService accessService,
        IThemePackCatalog themePackCatalog,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var membership = accessResult.Membership!;
        if (!membership.PermissionRole.CanManageOrganization())
        {
            return Results.Forbid();
        }

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Results.ValidationProblem(
                ToValidationDictionary(validation.Errors));
        }

        var organization = await dbContext.Organizations.SingleOrDefaultAsync(
            item => item.Id == organizationId && !item.IsArchived,
            cancellationToken);
        if (organization is null)
        {
            return Results.NotFound();
        }

        ThemePackDefinition? themePack;
        if (string.IsNullOrWhiteSpace(request.ThemePackKey))
        {
            themePack = await ResolveThemeAsync(
                organization.ThemePackId,
                themePackCatalog,
                cancellationToken);
        }
        else
        {
            themePack = await themePackCatalog.FindByKeyAsync(
                request.ThemePackKey,
                cancellationToken);
        }

        if (themePack is null)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["ThemePackKey"] = ["The selected theme pack does not exist."]
                });
        }

        organization.Name = request.Name.Trim();
        organization.Description = NormalizeOptional(request.Description);
        organization.ThemePackId = themePack.Id;
        if (request.EnabledModules is not null)
        {
            organization.EnabledModules = OrganizationModuleKeys.Normalize(
                request.EnabledModules);
        }

        organization.Language = request.Language.Trim();
        organization.TimeZone = request.TimeZone.Trim();
        organization.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToResponse(
            organization,
            themePack,
            membership.PermissionRole,
            membership.VisibleTitle));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationDbContext dbContext,
        IOrganizationAccessService accessService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var accessResult = await GetMembershipAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (accessResult.Result is not null)
        {
            return accessResult.Result;
        }

        var membership = accessResult.Membership!;
        if (!membership.PermissionRole.CanManageOrganization())
        {
            return Results.Forbid();
        }

        var organization = await dbContext.Organizations.SingleOrDefaultAsync(
            item => item.Id == organizationId && !item.IsArchived,
            cancellationToken);
        if (organization is null)
        {
            return Results.NotFound();
        }

        organization.IsArchived = true;
        organization.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }

    private static async Task<MembershipResult> GetMembershipAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(principal, out var userId))
        {
            return new MembershipResult(null, Results.Unauthorized());
        }

        var membership = await accessService.GetActiveMembershipAsync(
            organizationId,
            userId,
            cancellationToken);
        return membership is null
            ? new MembershipResult(null, Results.NotFound())
            : new MembershipResult(membership, null);
    }

    private static async Task<ThemePackDefinition?> ResolveThemeAsync(
        Guid? themePackId,
        IThemePackCatalog catalog,
        CancellationToken cancellationToken)
    {
        if (themePackId is not null)
        {
            var selectedTheme = await catalog.FindByIdAsync(
                themePackId.Value,
                cancellationToken);
            if (selectedTheme is not null)
            {
                return selectedTheme;
            }
        }

        return await catalog.FindByKeyAsync(
            ThemePackSeeds.GenericCorporateKey,
            cancellationToken);
    }

    private static ThemePackDefinition ResolveTheme(
        Guid? themePackId,
        IReadOnlyDictionary<Guid, ThemePackDefinition> themePackById,
        ThemePackDefinition fallbackTheme) =>
        themePackId is not null
        && themePackById.TryGetValue(themePackId.Value, out var selectedTheme)
            ? selectedTheme
            : fallbackTheme;

    private static ThemePackDefinition? FindFallbackTheme(
        IEnumerable<ThemePackDefinition> themePacks) =>
        themePacks.FirstOrDefault(
            themePack => themePack.Key == ThemePackSeeds.GenericCorporateKey)
        ?? themePacks.FirstOrDefault();

    private static OrganizationResponse ToResponse(
        Organization organization,
        ThemePackDefinition themePack,
        PermissionRole permissionRole,
        string? visibleTitle) =>
        new(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.Description,
            organization.ThemePackId,
            themePack.Key,
            themePack.Version,
            organization.EnabledModules,
            organization.Language,
            organization.TimeZone,
            organization.OwnerUserId,
            organization.CreatedAt,
            organization.UpdatedAt,
            organization.IsArchived,
            permissionRole,
            visibleTitle);

    private static IResult ThemePacksUnavailable() =>
        Results.Problem(
            title: "Theme packs are not initialized.",
            statusCode: StatusCodes.Status503ServiceUnavailable);

    private static bool TryGetUserId(
        ClaimsPrincipal principal,
        out Guid userId) =>
        Guid.TryParse(
            principal.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Dictionary<string, string[]> ToValidationDictionary(
        IEnumerable<ValidationFailure> failures) =>
        failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());

    private sealed record MembershipResult(
        OrganizationMembership? Membership,
        IResult? Result);
}
