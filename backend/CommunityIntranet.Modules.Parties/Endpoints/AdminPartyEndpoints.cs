using System.Security.Claims;
using CommunityIntranet.Modules.Parties.Contracts;
using CommunityIntranet.Modules.Parties.Domain;
using CommunityIntranet.Modules.Parties.Persistence;
using CommunityIntranet.Modules.Parties.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Parties.Endpoints;

internal static class AdminPartyEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/parties")
            .WithTags("Parties")
            .RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);
        group.MapGet("/{partyId:guid}", GetAsync);
        group.MapPut("/{partyId:guid}", UpdateAsync);
        group.MapDelete("/{partyId:guid}", ArchiveAsync);
        group.MapPost("/{partyId:guid}/order-items", AddOrderItemAsync);
        group.MapPut("/{partyId:guid}/order-items/{itemId:guid}", UpdateOrderItemAsync);
        group.MapDelete("/{partyId:guid}/order-items/{itemId:guid}", DeleteOrderItemAsync);
        group.MapGet("/{partyId:guid}/orders", ListOrdersAsync);
        group.MapPatch("/{partyId:guid}/orders/{orderId:guid}", ChangeOrderStatusAsync);
        group.MapGet("/{partyId:guid}/guests", ListGuestsAsync);
        group.MapGet("/{partyId:guid}/media", ListMediaAsync);
        group.MapGet("/{partyId:guid}/media/{mediaId:guid}/content", GetMediaContentAsync);
        group.MapDelete("/{partyId:guid}/media/{mediaId:guid}", DeleteMediaAsync);
        group.MapGet("/{partyId:guid}/guestbook", ListGuestbookAsync);
        group.MapDelete("/{partyId:guid}/guestbook/{entryId:guid}", DeleteGuestbookAsync);
        group.MapGet("/{partyId:guid}/music-requests", ListMusicAsync);
        group.MapPatch("/{partyId:guid}/music-requests/{requestId:guid}", ChangeMusicStatusAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userId = PartyEndpointHelpers.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var parties = await dbContext.Parties.AsNoTracking()
            .Where(x => x.OwnerUserId == userId && !x.IsArchived)
            .OrderByDescending(x => x.StartAt)
            .ToArrayAsync(cancellationToken);
        var responses = new List<PartyResponse>(parties.Length);
        foreach (var party in parties)
        {
            responses.Add(await ToResponseAsync(dbContext, party, cancellationToken));
        }

        return Results.Ok(responses);
    }

    private static async Task<IResult> CreateAsync(
        CreatePartyRequest request,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var userId = PartyEndpointHelpers.GetUserId(principal);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var error = ValidateParty(request.Name, request.Type, request.StartAt, request.EndAt);
        if (error is not null)
        {
            return error;
        }

        var name = request.Name!.Trim();
        var slug = PartySlugGenerator.Create(name, request.StartAt.Year);
        while (await dbContext.Parties.AnyAsync(x => x.Slug == slug, cancellationToken))
        {
            slug = PartySlugGenerator.Create(name, request.StartAt.Year);
        }

        var now = timeProvider.GetUtcNow();
        var party = new Party
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId.Value,
            Name = name,
            Slug = slug,
            Description = PartyEndpointHelpers.Clean(request.Description, 2000),
            Type = request.Type!.Trim(),
            Location = PartyEndpointHelpers.Clean(request.Location, 240),
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            WelcomeText = PartyEndpointHelpers.Clean(request.WelcomeText, 1000),
            IsActive = request.IsActive,
            GuestsCanViewGallery = request.GuestsCanViewGallery,
            GuestsCanViewGuestbook = request.GuestsCanViewGuestbook,
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.Parties.Add(party);
        AddDefaultOrderItems(dbContext, party.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/parties/{party.Id}", await ToResponseAsync(dbContext, party, cancellationToken));
    }

    private static async Task<IResult> GetAsync(
        Guid partyId,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken);
        return party is null
            ? Results.NotFound()
            : Results.Ok(await ToResponseAsync(dbContext, party, cancellationToken));
    }

    private static async Task<IResult> UpdateAsync(
        Guid partyId,
        UpdatePartyRequest request,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var error = ValidateParty(request.Name, request.Type, request.StartAt, request.EndAt);
        if (error is not null)
        {
            return error;
        }

        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken, tracked: true);
        if (party is null)
        {
            return Results.NotFound();
        }

        party.Name = request.Name!.Trim();
        party.Description = PartyEndpointHelpers.Clean(request.Description, 2000);
        party.Type = request.Type!.Trim();
        party.Location = PartyEndpointHelpers.Clean(request.Location, 240);
        party.StartAt = request.StartAt;
        party.EndAt = request.EndAt;
        party.WelcomeText = PartyEndpointHelpers.Clean(request.WelcomeText, 1000);
        party.IsActive = request.IsActive;
        party.GuestsCanViewGallery = request.GuestsCanViewGallery;
        party.GuestsCanViewGuestbook = request.GuestsCanViewGuestbook;
        party.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(await ToResponseAsync(dbContext, party, cancellationToken));
    }

    private static async Task<IResult> ArchiveAsync(
        Guid partyId,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken, tracked: true);
        if (party is null)
        {
            return Results.NotFound();
        }

        party.IsArchived = true;
        party.IsActive = false;
        party.UpdatedAt = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AddOrderItemAsync(
        Guid partyId,
        UpsertPartyOrderItemRequest request,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return PartyEndpointHelpers.Validation("name", "Bitte gib einen Namen mit maximal 100 Zeichen an.");
        }

        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var item = new PartyOrderItem
        {
            Id = Guid.NewGuid(),
            PartyId = partyId,
            Name = request.Name.Trim(),
            Icon = PartyEndpointHelpers.Clean(request.Icon, 20),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        dbContext.PartyOrderItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/parties/{partyId}/order-items/{item.Id}", ToOrderItem(item));
    }

    private static async Task<IResult> UpdateOrderItemAsync(
        Guid partyId,
        Guid itemId,
        UpsertPartyOrderItemRequest request,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return PartyEndpointHelpers.Validation("name", "Bitte gib einen Namen mit maximal 100 Zeichen an.");
        }

        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var item = await dbContext.PartyOrderItems.SingleOrDefaultAsync(x => x.Id == itemId && x.PartyId == partyId, cancellationToken);
        if (item is null)
        {
            return Results.NotFound();
        }

        item.Name = request.Name.Trim();
        item.Icon = PartyEndpointHelpers.Clean(request.Icon, 20);
        item.SortOrder = request.SortOrder;
        item.IsActive = request.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToOrderItem(item));
    }

    private static async Task<IResult> DeleteOrderItemAsync(
        Guid partyId,
        Guid itemId,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var item = await dbContext.PartyOrderItems.SingleOrDefaultAsync(x => x.Id == itemId && x.PartyId == partyId, cancellationToken);
        if (item is null)
        {
            return Results.NotFound();
        }

        item.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListOrdersAsync(
        Guid partyId,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var orders = await (
            from order in dbContext.PartyOrders.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on order.GuestId equals guest.Id
            join orderItem in dbContext.PartyOrderItems.AsNoTracking() on order.OrderItemId equals orderItem.Id into orderItems
            from item in orderItems.DefaultIfEmpty()
            join claimedGuest in dbContext.PartyGuests.AsNoTracking() on order.ClaimedByGuestId equals claimedGuest.Id into claimedGuests
            from claimant in claimedGuests.DefaultIfEmpty()
            where order.PartyId == partyId
            orderby order.Status, order.CreatedAt
            select new PartyOrderResponse(
                order.Id, order.GuestId, guest.Name, order.ClaimedByGuestId,
                claimant == null ? null : claimant.Name, order.OrderItemId,
                item == null ? null : item.Name, item == null ? null : item.Icon,
                order.CustomText, order.Status, order.CreatedAt, order.ClaimedAt, order.CompletedAt))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(orders);
    }

    private static async Task<IResult> ChangeOrderStatusAsync(
        Guid partyId,
        Guid orderId,
        ChangePartyOrderStatusRequest request,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var order = await dbContext.PartyOrders.SingleOrDefaultAsync(x => x.Id == orderId && x.PartyId == partyId, cancellationToken);
        if (order is null)
        {
            return Results.NotFound();
        }

        order.Status = request.Status;
        if (request.Status is PartyOrderStatus.Open or PartyOrderStatus.Cancelled)
        {
            order.ClaimedByGuestId = null;
            order.ClaimedAt = null;
        }
        order.CompletedAt = request.Status == PartyOrderStatus.Done ? timeProvider.GetUtcNow() : null;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListGuestsAsync(Guid partyId, ClaimsPrincipal principal, IPartyDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var guests = await dbContext.PartyGuests.AsNoTracking()
            .Where(x => x.PartyId == partyId)
            .OrderByDescending(x => x.LastSeenAt)
            .Select(x => new PartyGuestResponse(x.Id, x.Name, x.FirstSeenAt, x.LastSeenAt))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(guests);
    }

    private static async Task<IResult> ListMediaAsync(Guid partyId, ClaimsPrincipal principal, IPartyDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(await QueryMedia(dbContext, partyId, $"/api/parties/{partyId}/media", cancellationToken));
    }

    private static async Task<IResult> GetMediaContentAsync(Guid partyId, Guid mediaId, ClaimsPrincipal principal, IPartyDbContext dbContext, IPartyMediaStorage storage, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var media = await dbContext.PartyMedia.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mediaId && x.PartyId == partyId, cancellationToken);
        if (media is null)
        {
            return Results.NotFound();
        }

        var stream = await storage.OpenReadAsync(media.StoragePath, cancellationToken);
        return stream is null ? Results.NotFound() : Results.Stream(stream, media.MimeType, enableRangeProcessing: media.MediaType == "video");
    }

    private static async Task<IResult> DeleteMediaAsync(Guid partyId, Guid mediaId, ClaimsPrincipal principal, IPartyDbContext dbContext, IPartyMediaStorage storage, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var media = await dbContext.PartyMedia.SingleOrDefaultAsync(x => x.Id == mediaId && x.PartyId == partyId, cancellationToken);
        if (media is null)
        {
            return Results.NotFound();
        }

        await storage.DeleteAsync(media.StoragePath, cancellationToken);
        dbContext.PartyMedia.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListGuestbookAsync(Guid partyId, ClaimsPrincipal principal, IPartyDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(await QueryGuestbook(dbContext, partyId, cancellationToken));
    }

    private static async Task<IResult> DeleteGuestbookAsync(Guid partyId, Guid entryId, ClaimsPrincipal principal, IPartyDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var entry = await dbContext.PartyGuestbookEntries.SingleOrDefaultAsync(x => x.Id == entryId && x.PartyId == partyId, cancellationToken);
        if (entry is null)
        {
            return Results.NotFound();
        }

        dbContext.PartyGuestbookEntries.Remove(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> ListMusicAsync(Guid partyId, ClaimsPrincipal principal, IPartyDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var requests = await (
            from request in dbContext.PartyMusicRequests.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on request.GuestId equals guest.Id
            where request.PartyId == partyId
            let voteCount = dbContext.PartyMusicVotes.Count(vote => vote.PartyMusicRequestId == request.Id)
            orderby request.Status, voteCount descending, request.CreatedAt
            select new PartyMusicResponse(
                request.Id, request.GuestId, guest.Name, request.Song, request.Artist,
                request.Comment, request.Status, request.CreatedAt, request.SpotifyTrackId,
                request.SpotifyUri, request.SpotifyAlbumImageUrl, request.DurationMs,
                request.SpotifyQueuedAt, voteCount, false))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(requests);
    }

    private static async Task<IResult> ChangeMusicStatusAsync(Guid partyId, Guid requestId, ChangePartyMusicStatusRequest request, ClaimsPrincipal principal, IPartyDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await PartyEndpointHelpers.GetOwnedPartyAsync(dbContext, partyId, principal, cancellationToken) is null)
        {
            return Results.NotFound();
        }

        var music = await dbContext.PartyMusicRequests.SingleOrDefaultAsync(x => x.Id == requestId && x.PartyId == partyId, cancellationToken);
        if (music is null)
        {
            return Results.NotFound();
        }

        music.Status = request.Status;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    internal static async Task<PartyMediaResponse[]> QueryMedia(IPartyDbContext dbContext, Guid partyId, string baseUrl, CancellationToken cancellationToken) =>
        await (
            from media in dbContext.PartyMedia.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on media.GuestId equals guest.Id
            where media.PartyId == partyId
            orderby media.CreatedAt descending
            select new PartyMediaResponse(media.Id, media.GuestId, guest.Name, media.MediaType, media.FileName, media.MimeType, media.Size, media.Caption, media.CreatedAt, $"{baseUrl}/{media.Id}/content"))
            .ToArrayAsync(cancellationToken);

    internal static async Task<PartyGuestbookEntryResponse[]> QueryGuestbook(IPartyDbContext dbContext, Guid partyId, CancellationToken cancellationToken) =>
        await (
            from entry in dbContext.PartyGuestbookEntries.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on entry.GuestId equals guest.Id
            where entry.PartyId == partyId
            orderby entry.CreatedAt descending
            select new PartyGuestbookEntryResponse(entry.Id, entry.GuestId, guest.Name, entry.Message, entry.CreatedAt))
            .ToArrayAsync(cancellationToken);

    private static async Task<PartyResponse> ToResponseAsync(IPartyDbContext dbContext, Party party, CancellationToken cancellationToken)
    {
        var guestCount = await dbContext.PartyGuests.AsNoTracking().CountAsync(x => x.PartyId == party.Id, cancellationToken);
        var openOrderCount = await dbContext.PartyOrders.AsNoTracking().CountAsync(x => x.PartyId == party.Id && x.Status == PartyOrderStatus.Open, cancellationToken);
        var items = await dbContext.PartyOrderItems.AsNoTracking()
            .Where(x => x.PartyId == party.Id)
            .OrderBy(x => x.SortOrder)
            .Select(x => new PartyOrderItemResponse(x.Id, x.Name, x.Icon, x.SortOrder, x.IsActive))
            .ToArrayAsync(cancellationToken);
        return new PartyResponse(party.Id, party.Name, party.Slug, party.Description, party.Type, party.Location, party.StartAt, party.EndAt, party.WelcomeText, party.IsActive, party.GuestsCanViewGallery, party.GuestsCanViewGuestbook, party.CreatedAt, party.UpdatedAt, guestCount, openOrderCount, items);
    }

    private static IResult? ValidateParty(string? name, string? type, DateTimeOffset startAt, DateTimeOffset? endAt)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 160)
        {
            return PartyEndpointHelpers.Validation("name", "Bitte gib einen Partynamen mit maximal 160 Zeichen an.");
        }
        if (string.IsNullOrWhiteSpace(type) || type.Trim().Length > 40)
        {
            return PartyEndpointHelpers.Validation("type", "Bitte wähle einen gültigen Party-Typ.");
        }
        if (endAt is not null && endAt <= startAt)
        {
            return PartyEndpointHelpers.Validation("endAt", "Das Ende muss nach dem Start liegen.");
        }
        return null;
    }

    private static PartyOrderItemResponse ToOrderItem(PartyOrderItem item) =>
        new(item.Id, item.Name, item.Icon, item.SortOrder, item.IsActive);

    private static void AddDefaultOrderItems(IPartyDbContext dbContext, Guid partyId)
    {
        var defaults = new (string Name, string Icon)[]
        {
            ("Bier", "🍺"), ("Radler", "🍻"), ("Wasser", "💧"),
            ("Cola", "🥤"), ("Cola Zero", "🥤"), ("Fanta", "🍊"),
            ("Sprite", "🍋"), ("Aperol", "🍹"), ("Wein", "🍷"), ("Shot", "🥃")
        };
        for (var index = 0; index < defaults.Length; index++)
        {
            dbContext.PartyOrderItems.Add(new PartyOrderItem
            {
                Id = Guid.NewGuid(), PartyId = partyId, Name = defaults[index].Name,
                Icon = defaults[index].Icon, SortOrder = index, IsActive = true
            });
        }
    }
}
