using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Organizations.Contracts;
using CommunityIntranet.Modules.Organizations.Domain;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.Organizations.Services;
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
        var response = organizations.Select(organization =>
        {
            var membership = membershipByOrganization[organization.Id];
            return new OrganizationSummaryResponse(
                organization.Id,
                organization.Name,
                organization.Slug,
                organization.Description,
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
            return Results.ValidationProblem(ToValidationDictionary(validation.Errors));
        }

        var baseSlug = SlugGenerator.Create(request.Name);
        var slug = baseSlug;
        if (await dbContext.Organizations.AnyAsync(
                organization => organization.Slug == slug,
                cancellationToken))
        {
            slug = $"{baseSlug}-{Guid.NewGuid():N}"[..Math.Min(baseSlug.Length + 9, 140)];
        }

        var now = timeProvider.GetUtcNow();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            Description = NormalizeOptional(request.Description),
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
                PermissionRole.Owner,
                NormalizeOptional(request.VisibleTitle)));
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationDbContext dbContext,
        IOrganizationAccessService accessService,
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

        return organization is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(
                organization,
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
            return Results.ValidationProblem(ToValidationDictionary(validation.Errors));
        }

        var organization = await dbContext.Organizations.SingleOrDefaultAsync(
            item => item.Id == organizationId && !item.IsArchived,
            cancellationToken);
        if (organization is null)
        {
            return Results.NotFound();
        }

        organization.Name = request.Name.Trim();
        organization.Description = NormalizeOptional(request.Description);
        organization.Language = request.Language.Trim();
        organization.TimeZone = request.TimeZone.Trim();
        organization.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Ok(ToResponse(
            organization,
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

    private static OrganizationResponse ToResponse(
        Organization organization,
        PermissionRole permissionRole,
        string? visibleTitle) =>
        new(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.Description,
            organization.ThemePackId,
            organization.Language,
            organization.TimeZone,
            organization.OwnerUserId,
            organization.CreatedAt,
            organization.UpdatedAt,
            organization.IsArchived,
            permissionRole,
            visibleTitle);

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
