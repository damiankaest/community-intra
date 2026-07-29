using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.AiAssistant.Contracts;
using CommunityIntranet.Modules.AiAssistant.Domain;
using CommunityIntranet.Modules.AiAssistant.Persistence;
using CommunityIntranet.Modules.AiAssistant.Services;
using CommunityIntranet.Modules.Organizations.Persistence;
using CommunityIntranet.Modules.Projects.Domain;
using CommunityIntranet.Modules.Tasks.Domain;
using CommunityIntranet.Modules.ThemePacks.Seeding;
using CommunityIntranet.Modules.ThemePacks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.AiAssistant.Endpoints;

public static class AiAssistantEndpoints
{
    private static readonly JsonSerializerOptions SerializerOptions = new(
        JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static IEndpointRouteBuilder MapAiAssistantEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/assistant")
            .WithTags("AI Assistant")
            .RequireAuthorization();

        group.MapGet("/availability", GetAvailabilityAsync);
        group.MapGet("/chat", GetChatAsync);
        group.MapPost("/chat/messages", StreamChatMessageAsync)
            .RequireRateLimiting("assistant");
        group.MapPost("/actions/{actionId:guid}/confirm", ConfirmActionAsync);
        group.MapPost("/work-plan-drafts", PrepareWorkPlanAsync)
            .RequireRateLimiting("assistant");
        group.MapPost(
            "/work-plan-drafts/{draftId:guid}/confirm",
            ConfirmWorkPlanAsync);
        return endpoints;
    }

    private static async Task<IResult> GetChatAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IAiAssistantDbContext dbContext,
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

        var membership = access.Membership!;
        var conversation = await dbContext.AssistantConversations
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.MemberId == membership.MemberId)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (conversation is null)
        {
            return Results.Ok(new AssistantConversationResponse(
                null,
                AssistantTone.Theme,
                [],
                []));
        }

        var messages = await dbContext.AssistantMessages
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.ConversationId == conversation.Id
                && item.MemberId == membership.MemberId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(40)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new AssistantMessageResponse(
                item.Id,
                item.Role,
                item.Content,
                item.CreatedAt))
            .ToArrayAsync(cancellationToken);
        var actions = await dbContext.AssistantActions
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.ConversationId == conversation.Id
                && item.RequestedByMemberId == membership.MemberId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(20)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(new AssistantConversationResponse(
            conversation.Id,
            conversation.Tone,
            messages,
            actions.Select(ToActionResponse).ToArray()));
    }

    private static async Task StreamChatMessageAsync(
        Guid organizationId,
        SendAssistantMessageRequest request,
        HttpContext httpContext,
        ClaimsPrincipal principal,
        IAiAssistantDbContext dbContext,
        IOrganizationDbContext organizationDbContext,
        IOrganizationAccessService accessService,
        IThemePackCatalog themePackCatalog,
        IWorkspaceChatGenerator generator,
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
            await access.Result.ExecuteAsync(httpContext);
            return;
        }

        var membership = access.Membership!;
        var content = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
        {
            await Validation(
                "Message",
                "Die Nachricht muss zwischen 1 und 2000 Zeichen enthalten.")
                .ExecuteAsync(httpContext);
            return;
        }

        if (!Enum.IsDefined(request.Tone))
        {
            await Validation("Tone", "Der gewählte Tonfall ist ungültig.")
                .ExecuteAsync(httpContext);
            return;
        }

        if (!generator.IsConfigured)
        {
            await Results.Problem(
                title: "KI-Assistent nicht konfiguriert",
                detail: "Auf dem Server fehlt der OpenAI API-Schlüssel.",
                statusCode: StatusCodes.Status503ServiceUnavailable)
                .ExecuteAsync(httpContext);
            return;
        }

        var organization = await organizationDbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == organizationId && !item.IsArchived,
                cancellationToken);
        if (organization is null)
        {
            await Results.NotFound().ExecuteAsync(httpContext);
            return;
        }

        var theme = organization.ThemePackId is null
            ? await themePackCatalog.FindByKeyAsync(
                ThemePackSeeds.GenericCorporateKey,
                cancellationToken)
            : await themePackCatalog.FindByIdAsync(
                organization.ThemePackId.Value,
                cancellationToken);
        if (theme is null)
        {
            await Results.Problem(
                title: "Theme Pack nicht verfügbar",
                statusCode: StatusCodes.Status503ServiceUnavailable)
                .ExecuteAsync(httpContext);
            return;
        }

        var now = timeProvider.GetUtcNow();
        var conversation = await dbContext.AssistantConversations
            .Where(item =>
                item.OrganizationId == organizationId
                && item.MemberId == membership.MemberId)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (conversation is null)
        {
            conversation = new AssistantConversation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                MemberId = membership.MemberId,
                Tone = request.Tone,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.AssistantConversations.Add(conversation);
        }
        else
        {
            conversation.Tone = request.Tone;
            conversation.UpdatedAt = now;
        }

        var userMessage = new AssistantMessage
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ConversationId = conversation.Id,
            MemberId = membership.MemberId,
            Role = AssistantMessageRole.User,
            Content = content,
            CreatedAt = now
        };
        dbContext.AssistantMessages.Add(userMessage);
        await dbContext.SaveChangesAsync(cancellationToken);

        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = "application/x-ndjson; charset=utf-8";
        httpContext.Response.Headers["Cache-Control"] = "no-cache, no-store";
        httpContext.Response.Headers["X-Accel-Buffering"] = "no";
        await WriteStreamEventAsync(
            httpContext,
            new
            {
                type = "message_ack",
                conversationId = conversation.Id,
                message = new AssistantMessageResponse(
                    userMessage.Id,
                    userMessage.Role,
                    userMessage.Content,
                    userMessage.CreatedAt)
            },
            cancellationToken);

        var history = await dbContext.AssistantMessages
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == organizationId
                && item.ConversationId == conversation.Id
                && item.MemberId == membership.MemberId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(30)
            .OrderBy(item => item.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var responseText = new StringBuilder();
        try
        {
            await foreach (var chatEvent in generator.StreamAsync(
                organizationId,
                membership.MemberId,
                conversation.Id,
                request.Tone,
                theme.Configuration,
                history,
                membership.PermissionRole.CanCreateContent(),
                cancellationToken))
            {
                if (chatEvent.Delta is not null)
                {
                    responseText.Append(chatEvent.Delta);
                    await WriteStreamEventAsync(
                        httpContext,
                        new { type = "delta", delta = chatEvent.Delta },
                        cancellationToken);
                }

                if (chatEvent.Action is not null)
                {
                    await WriteStreamEventAsync(
                        httpContext,
                        new
                        {
                            type = "action",
                            action = ToActionResponse(chatEvent.Action)
                        },
                        cancellationToken);
                }
            }

            var finalText = responseText.ToString().Trim();
            if (string.IsNullOrWhiteSpace(finalText))
            {
                finalText = "Ich habe die Anfrage verstanden, konnte aber keine Antwort formulieren.";
            }

            var completedAt = timeProvider.GetUtcNow();
            var assistantMessage = new AssistantMessage
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ConversationId = conversation.Id,
                MemberId = membership.MemberId,
                Role = AssistantMessageRole.Assistant,
                Content = Truncate(finalText, 12000),
                Model = generator.Model,
                CreatedAt = completedAt
            };
            conversation.UpdatedAt = completedAt;
            dbContext.AssistantMessages.Add(assistantMessage);
            await dbContext.SaveChangesAsync(cancellationToken);
            await WriteStreamEventAsync(
                httpContext,
                new
                {
                    type = "done",
                    message = new AssistantMessageResponse(
                        assistantMessage.Id,
                        assistantMessage.Role,
                        assistantMessage.Content,
                        assistantMessage.CreatedAt)
                },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (InvalidOperationException exception)
        {
            await WriteStreamEventAsync(
                httpContext,
                new { type = "error", message = exception.Message },
                cancellationToken);
        }
    }

    private static async Task<IResult> ConfirmActionAsync(
        Guid organizationId,
        Guid actionId,
        ConfirmAssistantActionRequest request,
        ClaimsPrincipal principal,
        IAiAssistantDbContext dbContext,
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

        var membership = access.Membership!;
        var action = await dbContext.AssistantActions.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId
                && item.Id == actionId
                && item.RequestedByMemberId == membership.MemberId,
            cancellationToken);
        if (action is null)
        {
            return Results.NotFound();
        }

        if (action.Status == AssistantActionStatus.Confirmed
            && action.ResultEntityId is not null)
        {
            return Results.Ok(new ConfirmedAssistantActionResponse(
                action.Id,
                action.Kind,
                action.ResultEntityId.Value,
                true));
        }

        if (action.Status != AssistantActionStatus.Pending)
        {
            return Results.Conflict(new
            {
                title = "Aktion ist nicht mehr offen",
                detail = "Bitte lass eine neue Änderung vorbereiten."
            });
        }

        if (action.ConcurrencyToken != request.ConcurrencyToken)
        {
            return Results.Conflict(new
            {
                title = "Aktion wurde verändert",
                detail = "Bitte lade den Chat neu."
            });
        }

        if (!membership.PermissionRole.CanCreateContent())
        {
            return Results.Forbid();
        }

        var now = timeProvider.GetUtcNow();
        var result = action.Kind switch
        {
            AssistantActionKind.CreateTask => await ConfirmCreateTaskAsync(
                organizationId,
                membership,
                action,
                dbContext,
                activityWriter,
                now,
                cancellationToken),
            AssistantActionKind.UpdateTask => await ConfirmUpdateTaskAsync(
                organizationId,
                membership,
                action,
                dbContext,
                accessService,
                activityWriter,
                now,
                cancellationToken),
            AssistantActionKind.CreateProject => ConfirmCreateProject(
                organizationId,
                membership,
                action,
                dbContext,
                activityWriter,
                now),
            _ => new ActionConfirmationResult(
                null,
                Results.Problem(
                    title: "Unbekannte Aktion",
                    statusCode: StatusCodes.Status422UnprocessableEntity))
        };
        if (result.Error is not null)
        {
            return result.Error;
        }

        action.Status = AssistantActionStatus.Confirmed;
        action.CompletedAt = now;
        action.ResultEntityId = result.EntityId;
        action.ConcurrencyToken = Guid.NewGuid();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new ConfirmedAssistantActionResponse(
            action.Id,
            action.Kind,
            result.EntityId!.Value,
            false));
    }

    private static async Task<ActionConfirmationResult> ConfirmCreateTaskAsync(
        Guid organizationId,
        OrganizationMembership membership,
        AssistantAction action,
        IAiAssistantDbContext dbContext,
        IActivityWriter activityWriter,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<CreateTaskActionPayload>(action);
        if (payload is null
            || string.IsNullOrWhiteSpace(payload.Title)
            || payload.Title.Trim().Length > 200
            || payload.Description?.Length > 4000)
        {
            return InvalidAction("Die vorbereitete Aufgabe ist ungültig.");
        }

        if (payload.ProjectId is not null
            && !await dbContext.Projects
                .AsNoTracking()
                .AnyAsync(
                    project =>
                        project.OrganizationId == organizationId
                        && project.Id == payload.ProjectId,
                    cancellationToken))
        {
            return InvalidAction(
                "Das ausgewählte Projekt existiert nicht mehr.");
        }

        if (payload.ParentTaskId is not null)
        {
            var parent = await dbContext.WorkTasks
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    task =>
                        task.OrganizationId == organizationId
                        && task.Id == payload.ParentTaskId,
                    cancellationToken);
            if (parent is null || parent.ParentTaskId is not null)
            {
                return InvalidAction(
                    "Die ausgewählte Hauptaufgabe existiert nicht mehr.");
            }

            if (payload.ProjectId is not null
                && parent.ProjectId != payload.ProjectId)
            {
                return InvalidAction(
                    "Subtask und Hauptaufgabe gehören nicht zum selben Projekt.");
            }
        }

        var task = new WorkTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = payload.ProjectId,
            ParentTaskId = payload.ParentTaskId,
            Title = payload.Title.Trim(),
            Description = Normalize(payload.Description),
            Status = WorkTaskStatus.Open,
            Priority = payload.Priority,
            CreatedByMemberId = membership.MemberId,
            DueDate = payload.DueDate,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.WorkTasks.Add(task);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "task.created",
            membership.MemberId,
            "task",
            task.Id,
            new Dictionary<string, string?> { ["taskTitle"] = task.Title }));
        return new ActionConfirmationResult(task.Id, null);
    }

    private static async Task<ActionConfirmationResult> ConfirmUpdateTaskAsync(
        Guid organizationId,
        OrganizationMembership membership,
        AssistantAction action,
        IAiAssistantDbContext dbContext,
        IOrganizationAccessService accessService,
        IActivityWriter activityWriter,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<UpdateTaskActionPayload>(action);
        if (payload is null)
        {
            return InvalidAction("Die vorbereitete Änderung ist ungültig.");
        }

        var task = await dbContext.WorkTasks.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId
                && item.Id == payload.TaskId,
            cancellationToken);
        if (task is null)
        {
            return InvalidAction("Die Aufgabe existiert nicht mehr.");
        }

        if (!membership.PermissionRole.CanManageContent()
            && task.CreatedByMemberId != membership.MemberId
            && task.AssignedMemberId != membership.MemberId)
        {
            return new ActionConfirmationResult(null, Results.Forbid());
        }

        if (payload.Title is not null)
        {
            var title = payload.Title.Trim();
            if (title.Length is < 1 or > 200)
            {
                return InvalidAction(
                    "Der neue Aufgabentitel ist ungültig.");
            }

            task.Title = title;
        }

        if (payload.Description is not null)
        {
            if (payload.Description.Length > 4000)
            {
                return InvalidAction(
                    "Die neue Beschreibung ist zu lang.");
            }

            task.Description = Normalize(payload.Description);
        }

        if (payload.AssignedMemberId is not null
            && !await accessService.IsActiveMemberAsync(
                organizationId,
                payload.AssignedMemberId.Value,
                cancellationToken))
        {
            return InvalidAction(
                "Das ausgewählte Mitglied ist nicht mehr aktiv.");
        }

        if (payload.Priority is not null)
        {
            task.Priority = payload.Priority.Value;
        }

        if (payload.AssignedMemberId is not null)
        {
            task.AssignedMemberId = payload.AssignedMemberId;
        }

        if (payload.DueDate is not null)
        {
            task.DueDate = payload.DueDate;
        }

        if (payload.Status is not null)
        {
            var wasDone = task.Status == WorkTaskStatus.Done;
            task.Status = payload.Status.Value;
            task.CompletedAt = task.Status == WorkTaskStatus.Done
                ? task.CompletedAt ?? now
                : null;
            if (!wasDone && task.Status == WorkTaskStatus.Done)
            {
                activityWriter.Add(new ActivityDraft(
                    organizationId,
                    "task.completed",
                    membership.MemberId,
                    "task",
                    task.Id,
                    new Dictionary<string, string?>
                    {
                        ["taskTitle"] = task.Title
                    }));
            }
        }

        task.UpdatedAt = now;
        task.ConcurrencyToken = Guid.NewGuid();
        return new ActionConfirmationResult(task.Id, null);
    }

    private static ActionConfirmationResult ConfirmCreateProject(
        Guid organizationId,
        OrganizationMembership membership,
        AssistantAction action,
        IAiAssistantDbContext dbContext,
        IActivityWriter activityWriter,
        DateTimeOffset now)
    {
        var payload = DeserializePayload<CreateProjectActionPayload>(action);
        if (payload is null
            || string.IsNullOrWhiteSpace(payload.Name)
            || payload.Name.Trim().Length > 200
            || payload.Description?.Length > 4000)
        {
            return InvalidAction("Das vorbereitete Projekt ist ungültig.");
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = payload.Name.Trim(),
            Description = Normalize(payload.Description),
            Status = ProjectStatus.Planned,
            Priority = payload.Priority,
            OwnerMemberId = membership.MemberId,
            StartDate = DateOnly.FromDateTime(now.UtcDateTime),
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.Projects.Add(project);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "project.created",
            membership.MemberId,
            "project",
            project.Id,
            new Dictionary<string, string?>
            {
                ["projectName"] = project.Name
            }));
        return new ActionConfirmationResult(project.Id, null);
    }

    private static T? DeserializePayload<T>(AssistantAction action)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                action.PayloadJson,
                SerializerOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static ActionConfirmationResult InvalidAction(string detail) =>
        new(
            null,
            Results.Problem(
                title: "Aktion kann nicht ausgeführt werden",
                detail: detail,
                statusCode: StatusCodes.Status422UnprocessableEntity));

    private static AssistantActionResponse ToActionResponse(
        AssistantAction action)
    {
        using var document = JsonDocument.Parse(action.PayloadJson);
        return new AssistantActionResponse(
            action.Id,
            action.Kind,
            action.Status,
            document.RootElement.Clone(),
            action.CreatedAt,
            action.CompletedAt,
            action.ResultEntityId,
            action.ConcurrencyToken);
    }

    private static async Task WriteStreamEventAsync<T>(
        HttpContext context,
        T value,
        CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            value,
            SerializerOptions,
            cancellationToken);
        await context.Response.WriteAsync("\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static async Task<IResult> GetAvailabilityAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        IWorkPlanGenerator generator,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        return access.Result
            ?? Results.Ok(new AiAssistantAvailabilityResponse(
                generator.IsConfigured,
                generator.Model));
    }

    private static async Task<IResult> PrepareWorkPlanAsync(
        Guid organizationId,
        PrepareWorkPlanRequest request,
        ClaimsPrincipal principal,
        IAiAssistantDbContext dbContext,
        IOrganizationDbContext organizationDbContext,
        IOrganizationAccessService accessService,
        IThemePackCatalog themePackCatalog,
        IWorkPlanGenerator generator,
        IOptions<AiAssistantOptions> options,
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

        var prompt = request.Prompt?.Trim();
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 2000)
        {
            return Validation(
                "Prompt",
                "Die Anfrage muss zwischen 3 und 2000 Zeichen enthalten.");
        }

        if (!Enum.IsDefined(request.Tone))
        {
            return Validation("Tone", "Der gewählte Tonfall ist ungültig.");
        }

        if (!generator.IsConfigured)
        {
            return Results.Problem(
                title: "KI-Assistent nicht konfiguriert",
                detail:
                    "Für die Entwurfserstellung fehlt der serverseitige API-Schlüssel.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var organization = await organizationDbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == organizationId && !item.IsArchived,
                cancellationToken);
        if (organization is null)
        {
            return Results.NotFound();
        }

        var theme = organization.ThemePackId is null
            ? await themePackCatalog.FindByKeyAsync(
                ThemePackSeeds.GenericCorporateKey,
                cancellationToken)
            : await themePackCatalog.FindByIdAsync(
                organization.ThemePackId.Value,
                cancellationToken);
        if (theme is null)
        {
            return Results.Problem(
                title: "Theme Pack nicht verfügbar",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var generated = await generator.GenerateAsync(
            prompt,
            request.Tone,
            theme.Configuration,
            cancellationToken);
        if (!generated.IsSuccess)
        {
            return Results.Problem(
                title: "KI-Entwurf fehlgeschlagen",
                detail: generated.Error,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var now = timeProvider.GetUtcNow();
        var lifetime = Math.Clamp(
            options.Value.DraftLifetimeMinutes,
            5,
            24 * 60);
        var draft = new WorkPlanDraft
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            CreatedByMemberId = access.Membership.MemberId,
            Prompt = prompt,
            Tone = request.Tone,
            ProposalJson = JsonSerializer.Serialize(
                generated.Proposal,
                SerializerOptions),
            Model = generator.Model,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(lifetime),
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.WorkPlanDrafts.Add(draft);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/assistant/work-plan-drafts/{draft.Id}",
            ToResponse(draft, generated.Proposal!));
    }

    private static async Task<IResult> ConfirmWorkPlanAsync(
        Guid organizationId,
        Guid draftId,
        ConfirmWorkPlanRequest request,
        ClaimsPrincipal principal,
        IAiAssistantDbContext dbContext,
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

        var draft = await dbContext.WorkPlanDrafts.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == draftId,
            cancellationToken);
        if (draft is null || draft.CreatedByMemberId != access.Membership.MemberId)
        {
            return Results.NotFound();
        }

        if (draft.ConfirmedAt is not null && draft.ProjectId is not null)
        {
            var existingTaskIds = await dbContext.WorkTasks
                .AsNoTracking()
                .Where(task =>
                    task.OrganizationId == organizationId
                    && task.ProjectId == draft.ProjectId
                    && task.CreatedByMemberId == draft.CreatedByMemberId
                    && task.CreatedAt == draft.ConfirmedAt)
                .OrderBy(task => task.CreatedAt)
                .Select(task => task.Id)
                .ToArrayAsync(cancellationToken);
            return Results.Ok(new ConfirmedWorkPlanResponse(
                draft.Id,
                draft.ProjectId.Value,
                existingTaskIds,
                true));
        }

        if (draft.ExpiresAt <= timeProvider.GetUtcNow())
        {
            return Results.Problem(
                title: "Entwurf abgelaufen",
                detail: "Bitte lasse einen neuen Entwurf erstellen.",
                statusCode: StatusCodes.Status410Gone);
        }

        if (draft.ConcurrencyToken != request.ConcurrencyToken)
        {
            return Results.Conflict(new
            {
                title = "Entwurf wurde verändert",
                detail = "Bitte lade den Entwurf neu."
            });
        }

        var proposal = JsonSerializer.Deserialize<WorkPlanProposal>(
            draft.ProposalJson,
            SerializerOptions);
        if (proposal is null)
        {
            return Results.Problem(
                title: "Entwurf ist nicht mehr lesbar",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        var guarded = WorkPlanProposalGuard.ValidateAndNormalize(proposal);
        if (!guarded.IsSuccess)
        {
            return Results.Problem(
                title: "Entwurf ist ungültig",
                detail: guarded.Error,
                statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        var normalizedProposal = guarded.Proposal!;
        var now = timeProvider.GetUtcNow();
        var project = CreateProject(
            organizationId,
            access.Membership.MemberId,
            normalizedProposal,
            now);
        var tasks = normalizedProposal.Tasks
            .Select(task => CreateTask(
                organizationId,
                project.Id,
                access.Membership.MemberId,
                task,
                now))
            .ToArray();
        dbContext.Projects.Add(project);
        dbContext.WorkTasks.AddRange(tasks);

        draft.ProjectId = project.Id;
        draft.ConfirmedAt = now;
        draft.ConcurrencyToken = Guid.NewGuid();

        activityWriter.Add(new ActivityDraft(
            organizationId,
            "assistant.work-plan-confirmed",
            access.Membership.MemberId,
            "project",
            project.Id,
            new Dictionary<string, string?>
            {
                ["projectName"] = project.Name,
                ["taskCount"] = tasks.Length.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
            }));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/organizations/{organizationId}/projects/{project.Id}",
            new ConfirmedWorkPlanResponse(
                draft.Id,
                project.Id,
                tasks.Select(task => task.Id).ToArray(),
                false));
    }

    private static Project CreateProject(
        Guid organizationId,
        Guid ownerMemberId,
        WorkPlanProposal proposal,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = proposal.Title,
            Description = BuildProjectDescription(proposal),
            Status = ProjectStatus.Planned,
            Priority = MapProjectPriority(proposal.Tasks),
            OwnerMemberId = ownerMemberId,
            StartDate = DateOnly.FromDateTime(now.UtcDateTime),
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid()
        };

    private static WorkTask CreateTask(
        Guid organizationId,
        Guid projectId,
        Guid memberId,
        WorkPlanTask task,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ProjectId = projectId,
            Title = task.Title,
            Description = BuildTaskDescription(task),
            Status = WorkTaskStatus.Open,
            Priority = task.Priority,
            CreatedByMemberId = memberId,
            CreatedAt = now,
            UpdatedAt = now,
            ConcurrencyToken = Guid.NewGuid()
        };

    private static ProjectPriority MapProjectPriority(
        IReadOnlyList<WorkPlanTask> tasks)
    {
        var priority = tasks.Max(task => task.Priority);
        return priority switch
        {
            WorkTaskPriority.Low => ProjectPriority.Low,
            WorkTaskPriority.Normal => ProjectPriority.Normal,
            WorkTaskPriority.High => ProjectPriority.High,
            WorkTaskPriority.Critical => ProjectPriority.Critical,
            _ => ProjectPriority.Normal
        };
    }

    private static string BuildProjectDescription(WorkPlanProposal proposal)
    {
        var builder = new StringBuilder();
        builder.AppendLine(proposal.ExecutiveSummary);
        builder.AppendLine();
        builder.AppendLine("Management-Mitteilung:");
        builder.AppendLine(proposal.ManagementMessage);
        if (proposal.Materials.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Ressourcen / Material:");
            foreach (var material in proposal.Materials)
            {
                builder.Append("- ");
                builder.Append(material.Quantity);
                builder.Append(" × ");
                builder.Append(material.Name);
                if (!string.IsNullOrWhiteSpace(material.Notes))
                {
                    builder.Append(" – ");
                    builder.Append(material.Notes);
                }

                builder.AppendLine();
            }
        }

        return Truncate(builder.ToString().Trim(), 4000);
    }

    private static string BuildTaskDescription(WorkPlanTask task)
    {
        if (task.AcceptanceCriteria.Count == 0)
        {
            return task.Description;
        }

        var builder = new StringBuilder(task.Description);
        builder.AppendLine();
        builder.AppendLine();
        builder.AppendLine("Abnahmekriterien:");
        foreach (var criterion in task.AcceptanceCriteria)
        {
            builder.Append("- [ ] ");
            builder.AppendLine(criterion);
        }

        return Truncate(builder.ToString().Trim(), 4000);
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength
            ? value
            : value[..maximumLength].TrimEnd();

    private static WorkPlanDraftResponse ToResponse(
        WorkPlanDraft draft,
        WorkPlanProposal proposal) =>
        new(
            draft.Id,
            draft.Tone,
            draft.Prompt,
            proposal,
            draft.Model,
            draft.CreatedAt,
            draft.ExpiresAt,
            draft.ConfirmedAt,
            draft.ProjectId,
            draft.ConcurrencyToken);

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

    private sealed record ActionConfirmationResult(
        Guid? EntityId,
        IResult? Error);
}
