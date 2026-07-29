using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.Notifications.Contracts;
using CommunityIntranet.Modules.Notifications.Domain;
using CommunityIntranet.Modules.Notifications.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Notifications.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/organizations/{organizationId:guid}/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();
        group.MapGet("/", ListAsync);
        group.MapGet("/summary", GetSummaryAsync);
        group.MapPost("/{notificationId:guid}/read", MarkReadAsync);
        group.MapPost("/read-all", MarkAllReadAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid organizationId,
        bool unreadOnly,
        int? limit,
        ClaimsPrincipal principal,
        INotificationDbContext dbContext,
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
        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(notification =>
                notification.OrganizationId == organizationId
                && notification.RecipientMemberId == membership.MemberId);
        if (unreadOnly)
        {
            query = query.Where(notification => notification.ReadAt == null);
        }

        var notifications = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(Math.Clamp(limit ?? 40, 1, 100))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(notifications.Select(ToResponse));
    }

    private static async Task<IResult> GetSummaryAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        INotificationDbContext dbContext,
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
        var count = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(
                notification =>
                    notification.OrganizationId == organizationId
                    && notification.RecipientMemberId
                    == membership.MemberId
                    && notification.ReadAt == null,
                cancellationToken);
        return Results.Ok(new NotificationSummaryResponse(count));
    }

    private static async Task<IResult> MarkReadAsync(
        Guid organizationId,
        Guid notificationId,
        ClaimsPrincipal principal,
        INotificationDbContext dbContext,
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

        var membership = access.Membership!;
        var notification = await dbContext.Notifications.SingleOrDefaultAsync(
            item =>
                item.OrganizationId == organizationId
                && item.Id == notificationId
                && item.RecipientMemberId == membership.MemberId,
            cancellationToken);
        if (notification is null)
        {
            return Results.NotFound();
        }

        notification.ReadAt ??= timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(notification));
    }

    private static async Task<IResult> MarkAllReadAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        INotificationDbContext dbContext,
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

        var membership = access.Membership!;
        var now = timeProvider.GetUtcNow();
        var notifications = await dbContext.Notifications
            .Where(item =>
                item.OrganizationId == organizationId
                && item.RecipientMemberId == membership.MemberId
                && item.ReadAt == null)
            .ToArrayAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            notification.ReadAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static NotificationResponse ToResponse(
        MemberNotification notification) =>
        new(
            notification.Id,
            notification.NotificationType,
            notification.Title,
            notification.Body,
            notification.EntityType,
            notification.EntityId,
            notification.ActorMemberId,
            notification.CreatedAt,
            notification.ReadAt);

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
