using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Projects.Contracts;
using CommunityIntranet.Modules.Projects.Domain;
using CommunityIntranet.Modules.Projects.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Projects.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/projects")
            .WithTags("Projects")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/{projectId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{projectId:guid}", UpdateAsync);
        group.MapDelete("/{projectId:guid}", CancelAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid organizationId,
        ProjectStatus? status,
        ProjectPriority? priority,
        Guid? ownerMemberId,
        ClaimsPrincipal principal,
        IProjectDbContext dbContext,
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

        var query = dbContext.Projects
            .AsNoTracking()
            .Where(project => project.OrganizationId == organizationId);
        if (status is not null)
        {
            query = query.Where(project => project.Status == status);
        }

        if (priority is not null)
        {
            query = query.Where(project => project.Priority == priority);
        }

        if (ownerMemberId is not null)
        {
            query = query.Where(project =>
                project.OwnerMemberId == ownerMemberId);
        }

        var projects = await query
            .OrderBy(project => project.Status)
            .ThenByDescending(project => project.Priority)
            .ThenBy(project => project.DueDate)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(projects.Select(ToResponse));
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        Guid projectId,
        ClaimsPrincipal principal,
        IProjectDbContext dbContext,
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

        var project = await dbContext.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.OrganizationId == organizationId
                    && item.Id == projectId,
                cancellationToken);
        return project is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(project));
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        SaveProjectRequest request,
        ClaimsPrincipal principal,
        IProjectDbContext dbContext,
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

        if (!access.Membership!.PermissionRole.CanCreateContent())
        {
            return Results.Forbid();
        }

        var validation = await ValidateAsync(
            organizationId,
            request,
            accessService,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var now = timeProvider.GetUtcNow();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = request.Name.Trim(),
            Description = Normalize(request.Description),
            Status = request.Status,
            Priority = request.Priority,
            OwnerMemberId = request.OwnerMemberId,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = request.Status == ProjectStatus.Completed ? now : null,
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.Projects.Add(project);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "project.created",
            access.Membership.MemberId,
            "project",
            project.Id,
            new Dictionary<string, string?> { ["projectName"] = project.Name }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/projects/{project.Id}",
            ToResponse(project));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid projectId,
        SaveProjectRequest request,
        ClaimsPrincipal principal,
        IProjectDbContext dbContext,
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

        var project = await dbContext.Projects.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == projectId,
            cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        if (!access.Membership!.PermissionRole.CanManageContent()
            && project.OwnerMemberId != access.Membership.MemberId)
        {
            return Results.Forbid();
        }

        if (request.ConcurrencyToken != project.ConcurrencyToken)
        {
            return Results.Conflict(new
            {
                title = "Project was changed",
                detail = "Reload the project before saving again."
            });
        }

        var validation = await ValidateAsync(
            organizationId,
            request,
            accessService,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var wasCompleted = project.Status == ProjectStatus.Completed;
        var now = timeProvider.GetUtcNow();
        project.Name = request.Name.Trim();
        project.Description = Normalize(request.Description);
        project.Status = request.Status;
        project.Priority = request.Priority;
        project.OwnerMemberId = request.OwnerMemberId;
        project.StartDate = request.StartDate;
        project.DueDate = request.DueDate;
        project.UpdatedAt = now;
        project.CompletedAt = request.Status == ProjectStatus.Completed
            ? project.CompletedAt ?? now
            : null;
        project.ConcurrencyToken = Guid.NewGuid();
        if (!wasCompleted && project.Status == ProjectStatus.Completed)
        {
            activityWriter.Add(new ActivityDraft(
                organizationId,
                "project.completed",
                access.Membership.MemberId,
                "project",
                project.Id,
                new Dictionary<string, string?>
                {
                    ["projectName"] = project.Name
                }));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(project));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid projectId,
        ClaimsPrincipal principal,
        IProjectDbContext dbContext,
        IOrganizationAccessService accessService,
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

        if (!access.Membership!.PermissionRole.CanManageContent())
        {
            return Results.Forbid();
        }

        var project = await dbContext.Projects.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == projectId,
            cancellationToken);
        if (project is null)
        {
            return Results.NotFound();
        }

        project.Status = ProjectStatus.Cancelled;
        project.CompletedAt = null;
        project.UpdatedAt = timeProvider.GetUtcNow();
        project.ConcurrencyToken = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult?> ValidateAsync(
        Guid organizationId,
        SaveProjectRequest request,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || request.Name.Trim().Length > 160)
        {
            return Validation("Name", "Name must contain 1 to 160 characters.");
        }

        if (Normalize(request.Description)?.Length > 4000)
        {
            return Validation(
                "Description",
                "Description may contain at most 4000 characters.");
        }

        if (!Enum.IsDefined(request.Status)
            || !Enum.IsDefined(request.Priority))
        {
            return Validation("Status", "Status or priority is invalid.");
        }

        if (request.DueDate is not null
            && request.StartDate is not null
            && request.DueDate < request.StartDate)
        {
            return Validation("DueDate", "Due date cannot precede start date.");
        }

        if (request.OwnerMemberId is not null
            && !await accessService.IsActiveMemberAsync(
                organizationId,
                request.OwnerMemberId.Value,
                cancellationToken))
        {
            return Validation(
                "OwnerMemberId",
                "The selected owner is not an active member.");
        }

        return null;
    }

    private static ProjectResponse ToResponse(Project project) =>
        new(
            project.Id,
            project.Name,
            project.Description,
            project.Status,
            project.Priority,
            project.OwnerMemberId,
            project.StartDate,
            project.DueDate,
            project.CreatedAt,
            project.UpdatedAt,
            project.CompletedAt,
            project.ConcurrencyToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
