using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Notifications;
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
    private const long MaximumScreenshotSize = 5 * 1024 * 1024;
    private const long MaximumThumbnailSize = 512 * 1024;
    private static readonly HashSet<string> AllowedScreenshotMediaTypes =
    [
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif"
    ];

    public static IEndpointRouteBuilder MapTaskEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/tasks")
            .WithTags("Tasks")
            .RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapGet("/{taskId:guid}", GetAsync);
        group.MapGet("/{taskId:guid}/details", GetDetailsAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{taskId:guid}", UpdateAsync);
        group.MapPatch("/{taskId:guid}/status", ChangeStatusAsync);
        group.MapPost("/{taskId:guid}/comments", AddCommentAsync);
        group.MapPost("/{taskId:guid}/attachments", AddAttachmentAsync)
            .DisableAntiforgery();
        group.MapGet(
            "/{taskId:guid}/attachments/{attachmentId:guid}/content",
            GetAttachmentContentAsync);
        group.MapGet(
            "/{taskId:guid}/attachments/{attachmentId:guid}/thumbnail",
            GetAttachmentThumbnailAsync);
        group.MapDelete("/{taskId:guid}", DeleteAsync);
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

    private static async Task<IResult> GetDetailsAsync(
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
        if (task is null)
        {
            return Results.NotFound();
        }

        var subtasks = await dbContext.WorkTasks
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.ParentTaskId == taskId)
            .OrderBy(item => item.Status)
            .ThenByDescending(item => item.Priority)
            .ThenBy(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var comments = await dbContext.TaskComments
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.TaskId == taskId)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new TaskCommentResponse(
                item.Id,
                item.TaskId,
                item.AuthorMemberId,
                null,
                item.Body,
                item.CreatedAt))
            .ToArrayAsync(cancellationToken);
        var attachments = await dbContext.TaskAttachments
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.TaskId == taskId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new TaskAttachmentResponse(
                item.Id,
                item.TaskId,
                item.UploadedByMemberId,
                null,
                item.FileName,
                item.MediaType,
                item.Size,
                item.CreatedAt,
                $"/api/organizations/{organizationId}/tasks/{taskId}/attachments/{item.Id}/content",
                item.ThumbnailContent == null
                    ? null
                    : $"/api/organizations/{organizationId}/tasks/{taskId}/attachments/{item.Id}/thumbnail"))
            .ToArrayAsync(cancellationToken);

        return Results.Ok(new TaskDetailsResponse(
            ToResponse(task),
            subtasks.Select(ToResponse).ToArray(),
            comments,
            attachments));
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        SaveTaskRequest request,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
        IProjectLookup projectLookup,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
        INotificationWriter notificationWriter,
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
            null,
            request,
            dbContext,
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
            ParentTaskId = request.ParentTaskId,
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
        if (task.AssignedMemberId is not null)
        {
            AddNotification(
                notificationWriter,
                task,
                task.AssignedMemberId.Value,
                access.Membership.MemberId,
                "task.assigned",
                "Neue Aufgabe für dich",
                task.Title);
        }

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
        INotificationWriter notificationWriter,
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
            taskId,
            request,
            dbContext,
            projectLookup,
            accessService,
            cancellationToken);
        if (validation is not null)
        {
            return validation;
        }

        var previousAssignee = task.AssignedMemberId;
        var previousStatus = task.Status;
        task.ProjectId = request.ProjectId;
        task.ParentTaskId = request.ParentTaskId;
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
        AddChangeNotifications(
            notificationWriter,
            task,
            previousAssignee,
            previousStatus,
            access.Membership!.MemberId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> AddCommentAsync(
        Guid organizationId,
        Guid taskId,
        AddTaskCommentRequest request,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
        IOrganizationAccessService accessService,
        INotificationWriter notificationWriter,
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

        var body = Normalize(request.Body);
        if (body is null || body.Length > 2000)
        {
            return Validation(
                "Body",
                "Ein Kommentar muss zwischen 1 und 2000 Zeichen enthalten.");
        }

        var mentionedMemberIds = request.MentionedMemberIds?
            .Distinct()
            .ToArray() ?? [];
        if (mentionedMemberIds.Length > 10)
        {
            return Validation(
                "MentionedMemberIds",
                "Pro Kommentar sind maximal 10 Erwähnungen möglich.");
        }

        foreach (var memberId in mentionedMemberIds)
        {
            if (!await accessService.IsActiveMemberAsync(
                    organizationId,
                    memberId,
                    cancellationToken))
            {
                return Validation(
                    "MentionedMemberIds",
                    "Eine erwähnte Person ist kein aktives Mitglied.");
            }
        }

        var actorMemberId = access.Membership!.MemberId;
        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TaskId = taskId,
            AuthorMemberId = actorMemberId,
            Body = body,
            CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.TaskComments.Add(comment);
        foreach (var memberId in mentionedMemberIds)
        {
            AddNotification(
                notificationWriter,
                task,
                memberId,
                actorMemberId,
                "task.mentioned",
                "Du wurdest erwähnt",
                $"{task.Title}: {body}");
        }

        var watchers = new[] { task.CreatedByMemberId, task.AssignedMemberId }
            .Where(memberId => memberId is not null)
            .Select(memberId => memberId!.Value)
            .Except(mentionedMemberIds)
            .Distinct();
        foreach (var memberId in watchers)
        {
            AddNotification(
                notificationWriter,
                task,
                memberId,
                actorMemberId,
                "task.commented",
                "Neuer Kommentar",
                $"{task.Title}: {body}");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/tasks/{taskId}/details",
            new TaskCommentResponse(
                comment.Id,
                taskId,
                comment.AuthorMemberId,
                null,
                comment.Body,
                comment.CreatedAt));
    }

    private static async Task<IResult> AddAttachmentAsync(
        Guid organizationId,
        Guid taskId,
        IFormFile file,
        IFormFile? thumbnail,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
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

        if (!await TaskExistsAsync(
                dbContext,
                organizationId,
                taskId,
                cancellationToken))
        {
            return Results.NotFound();
        }

        if (file.Length is <= 0 or > MaximumScreenshotSize
            || !AllowedScreenshotMediaTypes.Contains(file.ContentType))
        {
            return Validation(
                "File",
                "Erlaubt sind PNG, JPEG, WebP oder GIF bis maximal 5 MB.");
        }

        if (thumbnail is not null
            && (thumbnail.Length is <= 0 or > MaximumThumbnailSize
                || !AllowedScreenshotMediaTypes.Contains(thumbnail.ContentType)))
        {
            return Validation(
                "Thumbnail",
                "Das Vorschaubild muss ein unterstütztes Bild bis 512 KB sein.");
        }

        var attachmentCount = await dbContext.TaskAttachments
            .AsNoTracking()
            .CountAsync(
                item =>
                    item.OrganizationId == organizationId
                    && item.TaskId == taskId,
                cancellationToken);
        if (attachmentCount >= 20)
        {
            return Validation(
                "File",
                "Pro Aufgabe sind maximal 20 Screenshots möglich.");
        }

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream((int)file.Length);
        await input.CopyToAsync(buffer, cancellationToken);
        byte[]? thumbnailContent = null;
        if (thumbnail is not null)
        {
            await using var thumbnailInput = thumbnail.OpenReadStream();
            using var thumbnailBuffer = new MemoryStream((int)thumbnail.Length);
            await thumbnailInput.CopyToAsync(
                thumbnailBuffer,
                cancellationToken);
            thumbnailContent = thumbnailBuffer.ToArray();
        }

        var attachment = new TaskAttachment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TaskId = taskId,
            UploadedByMemberId = access.Membership!.MemberId,
            FileName = NormalizeFileName(file.FileName),
            MediaType = file.ContentType,
            Size = file.Length,
            Content = buffer.ToArray(),
            ThumbnailContent = thumbnailContent,
            ThumbnailMediaType = thumbnail?.ContentType,
            CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.TaskAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/tasks/{taskId}/attachments/{attachment.Id}/content",
            new TaskAttachmentResponse(
                attachment.Id,
                taskId,
                attachment.UploadedByMemberId,
                null,
                attachment.FileName,
                attachment.MediaType,
                attachment.Size,
                attachment.CreatedAt,
                $"/api/organizations/{organizationId}/tasks/{taskId}/attachments/{attachment.Id}/content",
                attachment.ThumbnailContent is null
                    ? null
                    : $"/api/organizations/{organizationId}/tasks/{taskId}/attachments/{attachment.Id}/thumbnail"));
    }

    private static async Task<IResult> GetAttachmentContentAsync(
        Guid organizationId,
        Guid taskId,
        Guid attachmentId,
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

        var attachment = await dbContext.TaskAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.OrganizationId == organizationId
                    && item.TaskId == taskId
                    && item.Id == attachmentId,
                cancellationToken);
        return attachment is null
            ? Results.NotFound()
            : Results.File(
                attachment.Content,
                attachment.MediaType,
                enableRangeProcessing: false);
    }

    private static async Task<IResult> GetAttachmentThumbnailAsync(
        Guid organizationId,
        Guid taskId,
        Guid attachmentId,
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

        var attachment = await dbContext.TaskAttachments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.OrganizationId == organizationId
                    && item.TaskId == taskId
                    && item.Id == attachmentId,
                cancellationToken);
        return attachment?.ThumbnailContent is null
            ? Results.NotFound()
            : Results.File(
                attachment.ThumbnailContent,
                attachment.ThumbnailMediaType ?? attachment.MediaType,
                enableRangeProcessing: false);
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid organizationId,
        Guid taskId,
        ChangeTaskStatusRequest request,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
        INotificationWriter notificationWriter,
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

        var previousStatus = task.Status;
        SetStatus(
            task,
            request.Status,
            organizationId,
            access.Membership!,
            activityWriter,
            timeProvider);
        AddChangeNotifications(
            notificationWriter,
            task,
            task.AssignedMemberId,
            previousStatus,
            access.Membership!.MemberId);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(task));
    }

    private static async Task<IResult> DeleteAsync(
        Guid organizationId,
        Guid taskId,
        ClaimsPrincipal principal,
        ITaskDbContext dbContext,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
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

        activityWriter.Add(new ActivityDraft(
            organizationId,
            "task.deleted",
            access.Membership!.MemberId,
            "task",
            task.Id,
            new Dictionary<string, string?> { ["taskTitle"] = task.Title }));
        dbContext.WorkTasks.Remove(task);
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

    private static void AddChangeNotifications(
        INotificationWriter notificationWriter,
        WorkTask task,
        Guid? previousAssignee,
        WorkTaskStatus previousStatus,
        Guid actorMemberId)
    {
        if (task.AssignedMemberId is not null
            && task.AssignedMemberId != previousAssignee)
        {
            AddNotification(
                notificationWriter,
                task,
                task.AssignedMemberId.Value,
                actorMemberId,
                "task.assigned",
                "Neue Aufgabe für dich",
                task.Title);
        }

        if (task.Status == previousStatus)
        {
            return;
        }

        var recipients = new[] { task.CreatedByMemberId, task.AssignedMemberId }
            .Where(memberId => memberId is not null)
            .Select(memberId => memberId!.Value)
            .Distinct();
        foreach (var recipientMemberId in recipients)
        {
            AddNotification(
                notificationWriter,
                task,
                recipientMemberId,
                actorMemberId,
                "task.status_changed",
                "Aufgabenstatus geändert",
                $"{task.Title} ist jetzt {StatusLabel(task.Status)}.");
        }
    }

    private static void AddNotification(
        INotificationWriter notificationWriter,
        WorkTask task,
        Guid recipientMemberId,
        Guid actorMemberId,
        string notificationType,
        string title,
        string body)
    {
        notificationWriter.Add(new NotificationDraft(
            task.OrganizationId,
            recipientMemberId,
            actorMemberId,
            notificationType,
            title,
            body,
            "task",
            task.Id));
    }

    private static string StatusLabel(WorkTaskStatus status) =>
        status switch
        {
            WorkTaskStatus.Open => "offen",
            WorkTaskStatus.InProgress => "in Arbeit",
            WorkTaskStatus.Blocked => "blockiert",
            WorkTaskStatus.Done => "erledigt",
            WorkTaskStatus.Cancelled => "abgebrochen",
            _ => status.ToString()
        };

    private static bool CanEdit(
        WorkTask task,
        OrganizationMembership membership) =>
        membership.PermissionRole.CanManageContent()
        || task.CreatedByMemberId == membership.MemberId
        || task.AssignedMemberId == membership.MemberId;

    private static async Task<IResult?> ValidateAsync(
        Guid organizationId,
        Guid? currentTaskId,
        SaveTaskRequest request,
        ITaskDbContext dbContext,
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

        if (request.ParentTaskId is not null)
        {
            if (request.ParentTaskId == currentTaskId)
            {
                return Validation(
                    "ParentTaskId",
                    "Eine Aufgabe kann nicht ihr eigener Subtask sein.");
            }

            var parent = await dbContext.WorkTasks
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.OrganizationId == organizationId
                        && item.Id == request.ParentTaskId,
                    cancellationToken);
            if (parent is null)
            {
                return Validation(
                    "ParentTaskId",
                    "Die übergeordnete Aufgabe existiert nicht.");
            }

            if (parent.ParentTaskId is not null)
            {
                return Validation(
                    "ParentTaskId",
                    "Subtasks können nicht weiter verschachtelt werden.");
            }

            if (parent.ProjectId != request.ProjectId)
            {
                return Validation(
                    "ProjectId",
                    "Subtask und Hauptaufgabe müssen zum selben Projekt gehören.");
            }

            if (currentTaskId is not null
                && await dbContext.WorkTasks
                    .AsNoTracking()
                    .AnyAsync(
                        item =>
                            item.OrganizationId == organizationId
                            && item.ParentTaskId == currentTaskId,
                        cancellationToken))
            {
                return Validation(
                    "ParentTaskId",
                    "Eine Hauptaufgabe mit Subtasks kann nicht selbst zum Subtask werden.");
            }
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
            task.ParentTaskId,
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

    private static Task<bool> TaskExistsAsync(
        ITaskDbContext dbContext,
        Guid organizationId,
        Guid taskId,
        CancellationToken cancellationToken) =>
        dbContext.WorkTasks
            .AsNoTracking()
            .AnyAsync(
                item =>
                    item.OrganizationId == organizationId && item.Id == taskId,
                cancellationToken);

    private static string NormalizeFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName).Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? "screenshot.png"
            : normalized[..Math.Min(normalized.Length, 240)];
    }

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
