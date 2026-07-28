using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Incidents.Contracts;
using CommunityIntranet.Modules.Incidents.Domain;
using CommunityIntranet.Modules.Incidents.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Incidents.Endpoints;

public static class IncidentEndpoints
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/incidents")
            .WithTags("Incidents")
            .RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapGet("/{incidentId:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{incidentId:guid}", UpdateAsync);
        group.MapPost("/{incidentId:guid}/resolve", ResolveAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid organizationId,
        IncidentStatus? status,
        IncidentSeverity? severity,
        ClaimsPrincipal principal,
        IIncidentDbContext dbContext,
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

        var query = dbContext.Incidents
            .AsNoTracking()
            .Where(incident => incident.OrganizationId == organizationId);
        if (status is not null)
        {
            query = query.Where(incident => incident.Status == status);
        }

        if (severity is not null)
        {
            query = query.Where(incident => incident.Severity == severity);
        }

        var incidents = await query
            .OrderBy(incident => incident.Status)
            .ThenByDescending(incident => incident.Severity)
            .ThenByDescending(incident => incident.OccurredAt)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(incidents.Select(ToResponse));
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        Guid incidentId,
        ClaimsPrincipal principal,
        IIncidentDbContext dbContext,
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

        var incident = await dbContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.OrganizationId == organizationId
                    && item.Id == incidentId,
                cancellationToken);
        return incident is null
            ? Results.NotFound()
            : Results.Ok(ToResponse(incident));
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        SaveIncidentRequest request,
        ClaimsPrincipal principal,
        IIncidentDbContext dbContext,
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
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Severity = request.Severity,
            Status = request.Status,
            ReportedByMemberId = access.Membership.MemberId,
            ResponsibleMemberId = request.ResponsibleMemberId,
            Resolution = Normalize(request.Resolution),
            LessonsLearned = Normalize(request.LessonsLearned),
            OccurredAt = request.OccurredAt.ToUniversalTime(),
            CreatedAt = now,
            UpdatedAt = now,
            ResolvedAt = request.Status == IncidentStatus.Resolved ? now : null,
            ConcurrencyToken = Guid.NewGuid()
        };
        dbContext.Incidents.Add(incident);
        activityWriter.Add(new ActivityDraft(
            organizationId,
            "incident.reported",
            access.Membership.MemberId,
            "incident",
            incident.Id,
            new Dictionary<string, string?>
            {
                ["incidentTitle"] = incident.Title,
                ["severity"] = incident.Severity.ToString()
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created(
            $"/api/organizations/{organizationId}/incidents/{incident.Id}",
            ToResponse(incident));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid incidentId,
        SaveIncidentRequest request,
        ClaimsPrincipal principal,
        IIncidentDbContext dbContext,
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

        var incident = await dbContext.Incidents.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == incidentId,
            cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        if (!access.Membership!.PermissionRole.CanManageContent()
            && incident.ReportedByMemberId != access.Membership.MemberId)
        {
            return Results.Forbid();
        }

        if (request.ConcurrencyToken != incident.ConcurrencyToken)
        {
            return Conflict();
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

        var wasResolved = incident.Status == IncidentStatus.Resolved;
        var now = timeProvider.GetUtcNow();
        incident.Title = request.Title.Trim();
        incident.Description = request.Description.Trim();
        incident.Category = request.Category.Trim();
        incident.Severity = request.Severity;
        incident.Status = request.Status;
        incident.ResponsibleMemberId = request.ResponsibleMemberId;
        incident.Resolution = Normalize(request.Resolution);
        incident.LessonsLearned = Normalize(request.LessonsLearned);
        incident.OccurredAt = request.OccurredAt.ToUniversalTime();
        incident.UpdatedAt = now;
        incident.ResolvedAt = request.Status == IncidentStatus.Resolved
            ? incident.ResolvedAt ?? now
            : null;
        incident.ConcurrencyToken = Guid.NewGuid();
        AddResolvedActivity(
            incident,
            wasResolved,
            access.Membership,
            activityWriter);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(incident));
    }

    private static async Task<IResult> ResolveAsync(
        Guid organizationId,
        Guid incidentId,
        ResolveIncidentRequest request,
        ClaimsPrincipal principal,
        IIncidentDbContext dbContext,
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

        if (!access.Membership!.PermissionRole.CanManageContent())
        {
            return Results.Forbid();
        }

        var incident = await dbContext.Incidents.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId && item.Id == incidentId,
            cancellationToken);
        if (incident is null)
        {
            return Results.NotFound();
        }

        if (request.ConcurrencyToken != incident.ConcurrencyToken)
        {
            return Conflict();
        }

        if (string.IsNullOrWhiteSpace(request.Resolution)
            || request.Resolution.Trim().Length > 6000)
        {
            return Validation(
                "Resolution",
                "Resolution must contain 1 to 6000 characters.");
        }

        var wasResolved = incident.Status == IncidentStatus.Resolved;
        incident.Status = IncidentStatus.Resolved;
        incident.Resolution = request.Resolution.Trim();
        incident.LessonsLearned = Normalize(request.LessonsLearned);
        incident.ResolvedAt = timeProvider.GetUtcNow();
        incident.UpdatedAt = incident.ResolvedAt.Value;
        incident.ConcurrencyToken = Guid.NewGuid();
        AddResolvedActivity(
            incident,
            wasResolved,
            access.Membership,
            activityWriter);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(incident));
    }

    private static void AddResolvedActivity(
        Incident incident,
        bool wasResolved,
        OrganizationMembership membership,
        IActivityWriter activityWriter)
    {
        if (wasResolved || incident.Status != IncidentStatus.Resolved)
        {
            return;
        }

        activityWriter.Add(new ActivityDraft(
            incident.OrganizationId,
            "incident.resolved",
            membership.MemberId,
            "incident",
            incident.Id,
            new Dictionary<string, string?>
            {
                ["incidentTitle"] = incident.Title
            }));
    }

    private static async Task<IResult?> ValidateAsync(
        Guid organizationId,
        SaveIncidentRequest request,
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

        if (string.IsNullOrWhiteSpace(request.Description)
            || request.Description.Trim().Length > 6000)
        {
            return Validation(
                "Description",
                "Description must contain 1 to 6000 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Category)
            || request.Category.Trim().Length > 120)
        {
            return Validation(
                "Category",
                "Category must contain 1 to 120 characters.");
        }

        if (!Enum.IsDefined(request.Status)
            || !Enum.IsDefined(request.Severity))
        {
            return Validation("Status", "Status or severity is invalid.");
        }

        if (request.Status == IncidentStatus.Resolved
            && string.IsNullOrWhiteSpace(request.Resolution))
        {
            return Validation(
                "Resolution",
                "A resolved incident requires a resolution.");
        }

        if (request.ResponsibleMemberId is not null
            && !await accessService.IsActiveMemberAsync(
                organizationId,
                request.ResponsibleMemberId.Value,
                cancellationToken))
        {
            return Validation(
                "ResponsibleMemberId",
                "The responsible person is not an active member.");
        }

        return null;
    }

    private static IncidentResponse ToResponse(Incident incident) =>
        new(
            incident.Id,
            incident.Title,
            incident.Description,
            incident.Category,
            incident.Severity,
            incident.Status,
            incident.ReportedByMemberId,
            incident.ResponsibleMemberId,
            incident.Resolution,
            incident.LessonsLearned,
            incident.OccurredAt,
            incident.CreatedAt,
            incident.UpdatedAt,
            incident.ResolvedAt,
            incident.ConcurrencyToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IResult Conflict() =>
        Results.Conflict(new
        {
            title = "Incident was changed",
            detail = "Reload the incident before saving again."
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
