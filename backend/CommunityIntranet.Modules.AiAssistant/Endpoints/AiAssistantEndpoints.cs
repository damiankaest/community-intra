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
        group.MapPost("/work-plan-drafts", PrepareWorkPlanAsync)
            .RequireRateLimiting("assistant");
        group.MapPost(
            "/work-plan-drafts/{draftId:guid}/confirm",
            ConfirmWorkPlanAsync);
        return endpoints;
    }

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
}
