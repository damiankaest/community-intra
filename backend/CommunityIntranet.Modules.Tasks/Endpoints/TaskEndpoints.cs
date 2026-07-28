using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Projects.Services;
using CommunityIntranet.Modules.Tasks.Contracts;
using CommunityIntranet.Modules.Tasks.Domain;
using CommunityIntranet.Modules.Tasks.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Tasks.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapGet("/{taskId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{taskId:guid}", UpdateAsync);
        group.MapPatch("/{taskId:guid}/status", ChangeStatusAsync);
        group.MapDelete("/{taskId:guid}", CancelAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid organizationId,
        WorkTaskStatus? status,
        WorkTaskPriority? priority,
        Guid? projectId,
        Guid? assignedMemberId,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
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

        var query = dbContext.WorkTasks
            .AsNoTracking()
            .Where(task => task.OrganizationId == organizationId);
        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        if (priority is not null)
        {
            query = query.Where(task => task.Priority == priority);
        }

        if (projectId is not null)
        {
            query = query.Where(task => task.ProjectId == projectId);
        }

        if (assignedMemberId is not null)
        {
            query = query.Where(task =>
                task.AssignedMemberId == assignedMemberId);
        }

        var tasks = await query
            .OrderBy(task => task.Status)
            .ThenByDescending(task => task.Priority)
            .ThenBy(task => task.DueDate)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(tasks.Select(ToResponse));
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        Guid taskId,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
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

        var task = await dbContext.WorkTasks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.OrganizationId == organizationId && item.Id == taskId,
                cancellationToken);
        return task is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        SaveTaskRequest request,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
        IProjectLookup projectLookup,
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
            projectLookup,
            accessService,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var now = timeProvider.GetUtcNow();
        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = Normalize(request.Description),
            Status = request.Status,
            Priority = request.Priority,
            AssignedMemberId = request.AssignedMemberId,
            CreatedByMemberId = access.Membership.MemberId,
            DueDate = request.DueDate,
            CreatedAt = now,
            UpdatedAt = now,
            CompletedAt = request.Status == WorkTaskStatus.Done ? now : null,
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.WorkTasks.Add(task);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "task.created",
            access.Membership.MemberId,
            "task",
            task.Id,
            new Dictionary<string, string?> { ["taskTitle"] = task.Title }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/tasks/{task.Id}",
            ToResponse(task));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid taskId,
        SaveTaskRequest request,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
        IProjectLookup projectLookup,
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

        var task = await dbContext.WorkTasks.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == taskId,
            cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        if (!CanEdit(task, access.Membership!))
        {
            return Results.Forbid();
        }

        if (request.ConcurrencyToken != task.ConcurrencyToken)
        {
            return Conflict();
        }

        var validation = await ValidateAsync(
            organizationId,
            request,
            projectLookup,
            accessService,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        task.ProjectId = request.ProjectId;
        task.Title = request.Title.Trim();
        task.Description = Normalize(request.Description);
        task.Priority = request.Priority;
        task.AssignedMemberId = request.AssignedMemberId;
        task.DueDate = request.DueDate;
        SetStatus(
            task,
            request.Status,
            organizationId,
            access.Membership!,
            activityWriter,
            timeProvider);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid organizationId,
        Guid taskId,
        ChangeTaskStatusRequest request,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
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

        var task = await dbContext.WorkTasks.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == taskId,
            cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        if (!CanEdit(task, access.Membership!))
        {
            return Results.Forbid();
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Validation("Status", "Status is invalid.");
        }

        if (request.ConcurrencyToken != task.ConcurrencyToken)
        {
            return Conflict();
        }

        SetStatus(
            task,
            request.Status,
            organizationId,
            access.Membership!,
            activityWriter,
            timeProvider);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid taskId,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
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

        var task = await dbContext.WorkTasks.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == taskId,
            cancellationToken);
        if (task is null)
        {
            return Results.NotFound();
        }

        if (!CanEdit(task, access.Membership!))
        {
            return Results.Forbid();
        }

        SetStatus(
            task,
            WorkTaskStatus.Cancelled,
            organizationId,
            access.Membership!,
            activityWriter,
            timeProvider);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static void SetStatus(
        WorkTask task,
        WorkTaskStatus status,
        Guid organizationId,
        OrganizationMembership membership,
        IActivityWriter activityWriter,
        TimeProvider timeProvider)
    {
        var wasDone = task.Status == WorkTaskStatus.Done;
        var now = timeProvider.GetUtcNow();
        task.Status = status;
        task.UpdatedAt = now;
        task.CompletedAt = status == WorkTaskStatus.Done
            ? task.CompletedAt ?? now
            : null;
        task.ConcurrencyToken = Guid.NewGuid();
        if (!wasDone && status == WorkTaskStatus.Done)
        {
            activityWriter.Add(new ActivityDraft(
                organizationId,
                "task.completed",
                membership.MemberId,
                "task",
                task.Id,
                new Dictionary<string, string?> { ["taskTitle"] = task.Title }));
        }
    }

    private static bool CanEdit(
        WorkTask task,
        OrganizationMembership membership) =>
        membership.PermissionRole.CanManageContent()
        || task.CreatedByMemberId == membership.MemberId
        || task.AssignedMemberId == membership.MemberId;

    private static async Task<IResult?> ValidateAsync(
        Guid organizationId,
        SaveTaskRequest request,
        IProjectLookup projectLookup,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title)
            || request.Title.Trim().Length > 200)
        {
            return Validation(
                "Title",
                "Title must contain 1 to 200 characters.");
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

        if (request.ProjectId is not null
            && !await projectLookup.ExistsAsync(
                organizationId,
                request.ProjectId.Value,
                cancellationToken))
        {
            return Validation(
                "ProjectId",
                "The selected project does not exist.");
        }

        if (request.AssignedMemberId is not null
            && !await accessService.IsActiveMemberAsync(
                organizationId,
                request.AssignedMemberId.Value,
                cancellationToken))
        {
            return Validation(
                "AssignedMemberId",
                "The selected assignee is not an active member.");
        }

        return null;
    }

    private static TaskResponse ToResponse(WorkTask task) =>
        new(
            task.Id,
            task.ProjectId,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssignedMemberId,
            task.CreatedByMemberId,
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt,
            task.CompletedAt,
            task.ConcurrencyToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IResult Conflict() =>
        Results.Conflict(new
        {
            title = "Task was changed",
            detail = "Reload the task before saving again."
        });

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
