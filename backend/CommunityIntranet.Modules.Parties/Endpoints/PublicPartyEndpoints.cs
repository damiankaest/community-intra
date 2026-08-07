using CommunityIntranet.Modules.Parties.Contracts;
using CommunityIntranet.Modules.Parties.Domain;
using CommunityIntranet.Modules.Parties.Persistence;
using CommunityIntranet.Modules.Parties.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Parties.Endpoints;

internal static class PublicPartyEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/parties/public/{slug}")
            .WithTags("Party Guest")
            .RequireRateLimiting("party-public");

        group.MapGet("/", GetPartyAsync);
        group.MapGet("/manifest.webmanifest", GetManifestAsync);
        group.MapPost("/guests", RegisterGuestAsync);
        group.MapGet("/guests/me", GetGuestAsync);
        group.MapPatch("/guests/me", UpdateGuestAsync);
        group.MapGet("/orders", ListOrdersAsync);
        group.MapPost("/orders", CreateOrderAsync);
        group.MapPost("/orders/claim", ClaimOrdersAsync);
        group.MapPost("/orders/{orderId:guid}/release", ReleaseOrderAsync);
        group.MapPost("/orders/{orderId:guid}/done", CompleteClaimedOrderAsync);
        group.MapDelete("/orders/{orderId:guid}", CancelOwnOrderAsync);
        group.MapGet("/pulse", GetPulseAsync);
        group.MapGet("/feed", GetFeedAsync);
        group.MapGet("/media", ListMediaAsync);
        group.MapGet("/media/mine", ListOwnMediaAsync);
        group.MapPost("/media", UploadMediaAsync).DisableAntiforgery();
        group.MapPost("/media/{mediaId:guid}/like", ToggleMediaLikeAsync);
        group.MapGet("/media/{mediaId:guid}/content", GetMediaContentAsync);
        group.MapGet("/guestbook", ListGuestbookAsync);
        group.MapPost("/guestbook", AddGuestbookAsync);
        group.MapPost("/music-requests", AddMusicAsync);
        group.MapGet("/music-requests", ListMusicAsync);
        group.MapGet("/music-requests/mine", ListOwnMusicAsync);
        group.MapPost("/music-requests/{requestId:guid}/vote", ToggleMusicVoteAsync);
        return endpoints;
    }

    private static async Task<IResult> GetPartyAsync(string slug, IPartyDbContext dbContext, CancellationToken cancellationToken)
    {
        var party = await dbContext.Parties.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && !x.IsArchived, cancellationToken);
        if (party is null)
        {
            return Results.NotFound();
        }

        var items = party.IsActive
            ? await dbContext.PartyOrderItems.AsNoTracking()
                .Where(x => x.PartyId == party.Id && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .Select(x => new PartyOrderItemResponse(x.Id, x.Name, x.Icon, x.SortOrder, x.IsActive))
                .ToArrayAsync(cancellationToken)
            : [];
        return Results.Ok(new PartyPublicResponse(
            party.Name, party.Slug, party.Description, party.Type, party.Location,
            party.StartAt, party.EndAt, party.WelcomeText, party.IsActive,
            party.GuestsCanViewGallery, party.GuestsCanViewGuestbook, items));
    }

    private static async Task<IResult> GetManifestAsync(
        string slug,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var party = await dbContext.Parties.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Slug == slug && !x.IsArchived, cancellationToken);
        if (party is null)
        {
            return Results.NotFound();
        }

        var shortName = party.Name[..Math.Min(party.Name.Length, 24)];
        return Results.Json(new
        {
            name = $"{party.Name} · Party",
            short_name = shortName,
            id = $"/party/{party.Slug}",
            start_url = $"/party/{party.Slug}",
            scope = "/party/",
            display = "standalone",
            theme_color = "#24162b",
            background_color = "#171326",
            icons = new[]
            {
                new { src = "/favicon.svg", sizes = "any", type = "image/svg+xml", purpose = "any" }
            }
        }, contentType: "application/manifest+json");
    }

    private static async Task<IResult> RegisterGuestAsync(
        string slug,
        RegisterPartyGuestRequest request,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var party = await dbContext.Parties.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && !x.IsArchived, cancellationToken);
        if (party is null)
        {
            return Results.NotFound();
        }
        if (!party.IsActive)
        {
            return Results.Conflict(new { message = "Diese Party ist aktuell nicht aktiv." });
        }
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return PartyEndpointHelpers.Validation("name", "Bitte gib deinen Namen mit maximal 100 Zeichen an.");
        }

        var rawToken = PartyTokenService.CreateToken();
        var now = timeProvider.GetUtcNow();
        var guest = new PartyGuest
        {
            Id = Guid.NewGuid(),
            PartyId = party.Id,
            Name = request.Name.Trim(),
            SessionTokenHash = PartyTokenService.Hash(rawToken),
            FirstSeenAt = now,
            LastSeenAt = now
        };
        dbContext.PartyGuests.Add(guest);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/parties/public/{slug}/guests/me", new PartyGuestSessionResponse(guest.Id, guest.Name, rawToken));
    }

    private static async Task<IResult> GetGuestAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        return denied ?? Results.Ok(new PartyGuestResponse(
            access.Guest!.Id,
            access.Guest.Name,
            access.Guest.FirstSeenAt,
            access.Guest.LastSeenAt));
    }

    private static async Task<IResult> UpdateGuestAsync(
        string slug,
        UpdatePartyGuestRequest request,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 100)
        {
            return PartyEndpointHelpers.Validation("name", "Bitte gib deinen Namen mit maximal 100 Zeichen an.");
        }

        access.Guest!.Name = request.Name.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new PartyGuestResponse(access.Guest.Id, access.Guest.Name, access.Guest.FirstSeenAt, access.Guest.LastSeenAt));
    }

    private static async Task<IResult> CreateOrderAsync(
        string slug,
        CreatePartyOrderRequest request,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        PartyOrderItem? item = null;
        if (request.OrderItemId is not null)
        {
            item = await dbContext.PartyOrderItems.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == request.OrderItemId && x.PartyId == access.Party!.Id && x.IsActive,
                cancellationToken);
            if (item is null)
            {
                return PartyEndpointHelpers.Validation("orderItemId", "Diese Option ist nicht verfügbar.");
            }
        }

        var customText = PartyEndpointHelpers.Clean(request.CustomText, 160);
        if (item is null && customText is null)
        {
            return PartyEndpointHelpers.Validation("order", "Bitte wähle ein Getränk oder gib einen Wunsch an.");
        }

        var order = new PartyOrder
        {
            Id = Guid.NewGuid(),
            PartyId = access.Party!.Id,
            GuestId = access.Guest!.Id,
            OrderItemId = item?.Id,
            CustomText = customText,
            Status = PartyOrderStatus.Open,
            CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.PartyOrders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/parties/public/{slug}/orders/{order.Id}", new { order.Id, item = item?.Name ?? customText });
    }

    private static async Task<IResult> ListOrdersAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        return Results.Ok(await QueryOrdersAsync(dbContext, access.Party!.Id, access.Guest!.Id, cancellationToken));
    }

    private static async Task<IResult> ClaimOrdersAsync(
        string slug,
        ClaimPartyOrdersRequest request,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var orderIds = request.OrderIds?.Distinct().Take(12).ToArray() ?? [];
        if (orderIds.Length == 0)
        {
            return PartyEndpointHelpers.Validation("orderIds", "Wähle mindestens eine offene Bestellung aus.");
        }

        var now = timeProvider.GetUtcNow();
        var claimed = await dbContext.PartyOrders
            .Where(x => orderIds.Contains(x.Id)
                && x.PartyId == access.Party!.Id
                && x.GuestId != access.Guest!.Id
                && x.Status == PartyOrderStatus.Open
                && x.ClaimedByGuestId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ClaimedByGuestId, access.Guest!.Id)
                .SetProperty(x => x.ClaimedAt, now), cancellationToken);

        if (claimed == 0)
        {
            return Results.Conflict(new { message = "Diese Bestellung wurde gerade schon übernommen." });
        }

        return Results.Ok(new { claimed });
    }

    private static async Task<IResult> ReleaseOrderAsync(
        string slug,
        Guid orderId,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var changed = await dbContext.PartyOrders
            .Where(x => x.Id == orderId && x.PartyId == access.Party!.Id
                && x.Status == PartyOrderStatus.Open && x.ClaimedByGuestId == access.Guest!.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.ClaimedByGuestId, (Guid?)null)
                .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null), cancellationToken);
        return changed == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> CompleteClaimedOrderAsync(
        string slug,
        Guid orderId,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var now = timeProvider.GetUtcNow();
        var changed = await dbContext.PartyOrders
            .Where(x => x.Id == orderId && x.PartyId == access.Party!.Id
                && x.Status == PartyOrderStatus.Open && x.ClaimedByGuestId == access.Guest!.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PartyOrderStatus.Done)
                .SetProperty(x => x.CompletedAt, now), cancellationToken);
        return changed == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> CancelOwnOrderAsync(
        string slug,
        Guid orderId,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var changed = await dbContext.PartyOrders
            .Where(x => x.Id == orderId && x.PartyId == access.Party!.Id
                && x.GuestId == access.Guest!.Id && x.Status == PartyOrderStatus.Open)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PartyOrderStatus.Cancelled)
                .SetProperty(x => x.ClaimedByGuestId, (Guid?)null)
                .SetProperty(x => x.ClaimedAt, (DateTimeOffset?)null), cancellationToken);
        return changed == 0 ? Results.NotFound() : Results.NoContent();
    }

    private static async Task<IResult> GetPulseAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var partyId = access.Party!.Id;
        var guestCount = await dbContext.PartyGuests.CountAsync(x => x.PartyId == partyId, cancellationToken);
        var openOrderCount = await dbContext.PartyOrders.CountAsync(x => x.PartyId == partyId && x.Status == PartyOrderStatus.Open, cancellationToken);
        var unclaimedOrderCount = await dbContext.PartyOrders.CountAsync(x => x.PartyId == partyId && x.Status == PartyOrderStatus.Open && x.ClaimedByGuestId == null, cancellationToken);
        var mediaCount = await dbContext.PartyMedia.CountAsync(x => x.PartyId == partyId, cancellationToken);
        var openMusicRequestCount = await dbContext.PartyMusicRequests.CountAsync(x => x.PartyId == partyId && x.Status == PartyMusicRequestStatus.Open, cancellationToken);
        var guestbookEntryCount = await dbContext.PartyGuestbookEntries.CountAsync(x => x.PartyId == partyId, cancellationToken);
        var topDrink = await (
            from order in dbContext.PartyOrders.AsNoTracking()
            join item in dbContext.PartyOrderItems.AsNoTracking() on order.OrderItemId equals item.Id
            where order.PartyId == partyId && order.Status != PartyOrderStatus.Cancelled
            group order by item.Name into drinks
            orderby drinks.Count() descending
            select new { Name = drinks.Key, Count = drinks.Count() })
            .FirstOrDefaultAsync(cancellationToken);
        return Results.Ok(new PartyPulseResponse(
            guestCount, openOrderCount, unclaimedOrderCount, mediaCount,
            openMusicRequestCount, guestbookEntryCount,
            topDrink?.Name, topDrink?.Count ?? 0));
    }

    private static async Task<IResult> GetFeedAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var partyId = access.Party!.Id;
        var orderEvents = await (
            from order in dbContext.PartyOrders.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on order.GuestId equals guest.Id
            join orderItem in dbContext.PartyOrderItems.AsNoTracking() on order.OrderItemId equals orderItem.Id into orderItems
            from item in orderItems.DefaultIfEmpty()
            where order.PartyId == partyId
            orderby order.CreatedAt descending
            select new { guest.Name, Item = item == null ? order.CustomText : item.Name, order.CreatedAt })
            .Take(8).ToArrayAsync(cancellationToken);
        var claimEvents = await (
            from order in dbContext.PartyOrders.AsNoTracking()
            join recipient in dbContext.PartyGuests.AsNoTracking() on order.GuestId equals recipient.Id
            join claimant in dbContext.PartyGuests.AsNoTracking() on order.ClaimedByGuestId equals claimant.Id
            join orderItem in dbContext.PartyOrderItems.AsNoTracking() on order.OrderItemId equals orderItem.Id into orderItems
            from item in orderItems.DefaultIfEmpty()
            where order.PartyId == partyId && order.ClaimedAt != null
            orderby order.ClaimedAt descending
            select new { Claimant = claimant.Name, Recipient = recipient.Name, Item = item == null ? order.CustomText : item.Name, At = order.ClaimedAt!.Value })
            .Take(8).ToArrayAsync(cancellationToken);
        var mediaEvents = await (
            from media in dbContext.PartyMedia.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on media.GuestId equals guest.Id
            where media.PartyId == partyId
            orderby media.CreatedAt descending
            select new { guest.Name, media.MediaType, media.CreatedAt })
            .Take(8).ToArrayAsync(cancellationToken);
        var musicEvents = await (
            from music in dbContext.PartyMusicRequests.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on music.GuestId equals guest.Id
            where music.PartyId == partyId
            orderby music.CreatedAt descending
            select new { guest.Name, music.Song, music.CreatedAt })
            .Take(8).ToArrayAsync(cancellationToken);
        var guestbookEvents = await (
            from entry in dbContext.PartyGuestbookEntries.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on entry.GuestId equals guest.Id
            where entry.PartyId == partyId
            orderby entry.CreatedAt descending
            select new { guest.Name, entry.CreatedAt })
            .Take(8).ToArrayAsync(cancellationToken);

        var feed = orderEvents.Select(x => new PartyFeedItemResponse("order", "🍹", $"{x.Name} möchte {x.Item ?? "etwas zu trinken"}.", x.CreatedAt))
            .Concat(claimEvents.Select(x => new PartyFeedItemResponse("claim", "🏃", $"{x.Claimant} bringt {x.Recipient} {x.Item ?? "ein Getränk"}.", x.At)))
            .Concat(mediaEvents.Select(x => new PartyFeedItemResponse("media", x.MediaType == "video" ? "🎬" : "📸", $"{x.Name} hat { (x.MediaType == "video" ? "ein Video" : "ein Foto") } hochgeladen.", x.CreatedAt)))
            .Concat(musicEvents.Select(x => new PartyFeedItemResponse("music", "🎵", $"{x.Name} wünscht sich {x.Song}.", x.CreatedAt)))
            .Concat(guestbookEvents.Select(x => new PartyFeedItemResponse("guestbook", "💌", $"{x.Name} hat ins Gästebuch geschrieben.", x.CreatedAt)))
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToArray();
        return Results.Ok(feed);
    }

    private static async Task<PartyOrderResponse[]> QueryOrdersAsync(
        IPartyDbContext dbContext,
        Guid partyId,
        Guid currentGuestId,
        CancellationToken cancellationToken) =>
        await (
            from order in dbContext.PartyOrders.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on order.GuestId equals guest.Id
            join claimedGuest in dbContext.PartyGuests.AsNoTracking() on order.ClaimedByGuestId equals claimedGuest.Id into claimedGuests
            from claimant in claimedGuests.DefaultIfEmpty()
            join orderItem in dbContext.PartyOrderItems.AsNoTracking() on order.OrderItemId equals orderItem.Id into orderItems
            from item in orderItems.DefaultIfEmpty()
            where order.PartyId == partyId && (order.Status == PartyOrderStatus.Open || order.GuestId == currentGuestId)
            orderby order.CreatedAt descending
            select new PartyOrderResponse(
                order.Id, order.GuestId, guest.Name, order.ClaimedByGuestId,
                claimant == null ? null : claimant.Name, order.OrderItemId,
                item == null ? null : item.Name, item == null ? null : item.Icon,
                order.CustomText, order.Status, order.CreatedAt, order.ClaimedAt, order.CompletedAt))
        .Take(100)
        .ToArrayAsync(cancellationToken);

    private static async Task<IResult> UploadMediaAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        IPartyMediaStorage storage,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var requestSize = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (requestSize is { IsReadOnly: false })
        {
            requestSize.MaxRequestBodySize =
                PartyMediaValidator.MaximumVideoSize + 2 * 1024 * 1024;
        }

        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (!httpContext.Request.HasFormContentType)
        {
            return PartyEndpointHelpers.Validation("file", "Bitte wähle ein Foto oder Video aus.");
        }

        var form = await httpContext.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length <= 0)
        {
            return PartyEndpointHelpers.Validation("file", "Bitte wähle ein Foto oder Video aus.");
        }

        var mimeType = file.ContentType.Split(';')[0].Trim().ToLowerInvariant();
        var rule = PartyMediaValidator.GetRule(mimeType);
        if (rule is null)
        {
            return PartyEndpointHelpers.Validation("file", "Erlaubt sind JPEG, PNG, WebP, GIF, MP4, MOV oder WebM.");
        }
        if (file.Length > rule.MaximumSize)
        {
            var limit = rule.MediaType == "video" ? "100 MB" : "12 MB";
            return PartyEndpointHelpers.Validation("file", $"Die Datei ist zu groß. Maximum: {limit}.");
        }

        await using var input = file.OpenReadStream();
        if (!await PartyMediaValidator.HasValidSignatureAsync(input, mimeType, cancellationToken))
        {
            return PartyEndpointHelpers.Validation("file", "Dateiinhalt und Dateityp passen nicht zusammen.");
        }

        var storagePath = await storage.SaveAsync(access.Party!.Id, input, rule.Extension, cancellationToken);
        var media = new PartyMedia
        {
            Id = Guid.NewGuid(),
            PartyId = access.Party.Id,
            GuestId = access.Guest!.Id,
            MediaType = rule.MediaType,
            StoragePath = storagePath,
            FileName = NormalizeFileName(file.FileName, rule.Extension),
            MimeType = mimeType,
            Size = file.Length,
            Caption = PartyEndpointHelpers.Clean(form["caption"].ToString(), 500),
            CreatedAt = timeProvider.GetUtcNow()
        };
        try
        {
            dbContext.PartyMedia.Add(media);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(storagePath, CancellationToken.None);
            throw;
        }

        return Results.Created(
            $"/api/parties/public/{slug}/media/{media.Id}",
            new PartyMediaResponse(
                media.Id, media.GuestId, access.Guest.Name, media.MediaType,
                media.FileName, media.MimeType, media.Size, media.Caption,
                media.CreatedAt,
                $"/api/parties/public/{slug}/media/{media.Id}/content",
                0, false));
    }

    private static async Task<IResult> ListMediaAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (!access.Party!.GuestsCanViewGallery)
        {
            return Results.Forbid();
        }

        return Results.Ok(await AdminPartyEndpoints.QueryMedia(
            dbContext,
            access.Party.Id,
            $"/api/parties/public/{slug}/media",
            cancellationToken,
            access.Guest!.Id));
    }

    private static async Task<IResult> ListOwnMediaAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var party = access.Party!;
        var guest = access.Guest!;

        var media = await (
            from item in dbContext.PartyMedia.AsNoTracking()
            where item.PartyId == party.Id && item.GuestId == guest.Id
            let likeCount = dbContext.PartyMediaLikes.Count(like => like.PartyMediaId == item.Id)
            orderby item.CreatedAt descending
            select new PartyMediaResponse(
                item.Id, item.GuestId, guest.Name, item.MediaType,
                item.FileName, item.MimeType, item.Size, item.Caption, item.CreatedAt,
                $"/api/parties/public/{slug}/media/{item.Id}/content",
                likeCount,
                dbContext.PartyMediaLikes.Any(
                    like => like.PartyMediaId == item.Id && like.GuestId == guest.Id)))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(media);
    }

    private static async Task<IResult> GetMediaContentAsync(
        string slug,
        Guid mediaId,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        IPartyMediaStorage storage,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var party = access.Party!;
        var guest = access.Guest!;

        var media = await dbContext.PartyMedia.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mediaId && x.PartyId == party.Id, cancellationToken);
        if (media is null)
        {
            return Results.NotFound();
        }
        if (!party.GuestsCanViewGallery && media.GuestId != guest.Id)
        {
            return Results.Forbid();
        }
        var stream = await storage.OpenReadAsync(media.StoragePath, cancellationToken);
        return stream is null ? Results.NotFound() : Results.Stream(stream, media.MimeType, enableRangeProcessing: media.MediaType == "video");
    }

    private static async Task<IResult> ToggleMediaLikeAsync(
        string slug,
        Guid mediaId,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(
            dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var party = access.Party!;
        var guest = access.Guest!;
        if (!party.GuestsCanViewGallery)
        {
            return Results.Forbid();
        }
        if (!await dbContext.PartyMedia.AnyAsync(
            media => media.Id == mediaId && media.PartyId == party.Id,
            cancellationToken))
        {
            return Results.NotFound();
        }

        var like = await dbContext.PartyMediaLikes.SingleOrDefaultAsync(
            item => item.PartyMediaId == mediaId && item.GuestId == guest.Id,
            cancellationToken);
        var hasLiked = like is null;
        if (like is null)
        {
            dbContext.PartyMediaLikes.Add(new PartyMediaLike
            {
                PartyMediaId = mediaId,
                GuestId = guest.Id,
                CreatedAt = timeProvider.GetUtcNow()
            });
        }
        else
        {
            dbContext.PartyMediaLikes.Remove(like);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var likeCount = await dbContext.PartyMediaLikes.CountAsync(
            item => item.PartyMediaId == mediaId,
            cancellationToken);
        return Results.Ok(new PartyMediaLikeResponse(likeCount, hasLiked));
    }

    private static async Task<IResult> ListGuestbookAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (!access.Party!.GuestsCanViewGuestbook)
        {
            return Results.Forbid();
        }
        return Results.Ok(await AdminPartyEndpoints.QueryGuestbook(dbContext, access.Party.Id, cancellationToken));
    }

    private static async Task<IResult> AddGuestbookAsync(
        string slug,
        CreatePartyGuestbookEntryRequest request,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Trim().Length > 1000)
        {
            return PartyEndpointHelpers.Validation("message", "Bitte schreibe eine Nachricht mit maximal 1000 Zeichen.");
        }

        var entry = new PartyGuestbookEntry
        {
            Id = Guid.NewGuid(), PartyId = access.Party!.Id, GuestId = access.Guest!.Id,
            Message = request.Message.Trim(), CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.PartyGuestbookEntries.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/parties/public/{slug}/guestbook/{entry.Id}", new PartyGuestbookEntryResponse(entry.Id, entry.GuestId, access.Guest.Name, entry.Message, entry.CreatedAt));
    }

    private static async Task<IResult> AddMusicAsync(
        string slug,
        CreatePartyMusicRequest request,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        var party = access.Party!;
        var guest = access.Guest!;
        SpotifyTrackResponse? spotifyTrack = null;
        if (!string.IsNullOrWhiteSpace(request.SpotifyTrackId))
        {
            if (request.SpotifyTrackId.Trim().Length > 100
                || string.IsNullOrWhiteSpace(party.SpotifyProtectedRefreshToken))
            {
                return PartyEndpointHelpers.Validation("spotifyTrackId", "Spotify-Track ist ungültig oder Spotify ist nicht verbunden.");
            }
            try
            {
                spotifyTrack = await spotify.GetTrackAsync(
                    party,
                    request.SpotifyTrackId.Trim(),
                    cancellationToken);
            }
            catch (PartySpotifyException exception)
            {
                return Results.Problem(exception.Message, statusCode: StatusCodes.Status502BadGateway);
            }
            if (spotifyTrack is null)
            {
                return PartyEndpointHelpers.Validation("spotifyTrackId", "Spotify-Track wurde nicht gefunden.");
            }

            var duplicate = await dbContext.PartyMusicRequests.FirstOrDefaultAsync(
                x => x.PartyId == party.Id
                    && x.Status == PartyMusicRequestStatus.Open
                    && x.SpotifyTrackId == spotifyTrack.Id,
                cancellationToken);
            if (duplicate is not null)
            {
                if (!await dbContext.PartyMusicVotes.AnyAsync(
                    x => x.PartyMusicRequestId == duplicate.Id && x.GuestId == guest.Id,
                    cancellationToken))
                {
                    dbContext.PartyMusicVotes.Add(new PartyMusicVote
                    {
                        PartyMusicRequestId = duplicate.Id,
                        GuestId = guest.Id,
                        CreatedAt = timeProvider.GetUtcNow()
                    });
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                return Results.Ok(await ToMusicResponseAsync(
                    dbContext,
                    duplicate.Id,
                    guest.Id,
                    cancellationToken));
            }
        }

        var song = spotifyTrack?.Name ?? request.Song?.Trim();
        if (string.IsNullOrWhiteSpace(song) || song.Length > 200)
        {
            return PartyEndpointHelpers.Validation("song", "Bitte gib einen Song mit maximal 200 Zeichen an.");
        }

        var music = new PartyMusicRequest
        {
            Id = Guid.NewGuid(), PartyId = party.Id, GuestId = guest.Id,
            Song = song, Artist = spotifyTrack?.Artist ?? PartyEndpointHelpers.Clean(request.Artist, 200),
            Comment = PartyEndpointHelpers.Clean(request.Comment, 500),
            SpotifyTrackId = spotifyTrack?.Id,
            SpotifyUri = spotifyTrack?.Uri,
            SpotifyAlbumImageUrl = spotifyTrack?.AlbumImageUrl,
            DurationMs = spotifyTrack?.DurationMs,
            Status = PartyMusicRequestStatus.Open, CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.PartyMusicRequests.Add(music);
        dbContext.PartyMusicVotes.Add(new PartyMusicVote
        {
            PartyMusicRequestId = music.Id,
            GuestId = guest.Id,
            CreatedAt = music.CreatedAt
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        if (party.SpotifyAutoQueue && !string.IsNullOrWhiteSpace(music.SpotifyUri))
        {
            try
            {
                await spotify.AddToQueueAsync(party, music.SpotifyUri, cancellationToken);
                music.SpotifyQueuedAt = timeProvider.GetUtcNow();
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (PartySpotifyException)
            {
                // The wish remains visible and can still be queued manually by the admin.
            }
        }

        return Results.Created(
            $"/api/parties/public/{slug}/music-requests/{music.Id}",
            await ToMusicResponseAsync(dbContext, music.Id, guest.Id, cancellationToken));
    }

    private static async Task<IResult> ListMusicAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var guestId = access.Guest!.Id;
        var requests = await (
            from music in dbContext.PartyMusicRequests.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on music.GuestId equals guest.Id
            where music.PartyId == access.Party!.Id && music.Status != PartyMusicRequestStatus.Rejected
            let voteCount = dbContext.PartyMusicVotes.Count(vote => vote.PartyMusicRequestId == music.Id)
            orderby music.Status, voteCount descending, music.CreatedAt
            select new PartyMusicResponse(
                music.Id, music.GuestId, guest.Name, music.Song, music.Artist,
                music.Comment, music.Status, music.CreatedAt, music.SpotifyTrackId,
                music.SpotifyUri, music.SpotifyAlbumImageUrl, music.DurationMs,
                music.SpotifyQueuedAt, voteCount,
                dbContext.PartyMusicVotes.Any(vote => vote.PartyMusicRequestId == music.Id && vote.GuestId == guestId)))
            .Take(100)
            .ToArrayAsync(cancellationToken);
        return Results.Ok(requests);
    }

    private static async Task<IResult> ListOwnMusicAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var partyId = access.Party!.Id;
        var guestId = access.Guest!.Id;
        var guestName = access.Guest.Name;
        var requests = await dbContext.PartyMusicRequests.AsNoTracking()
            .Where(x => x.PartyId == partyId && x.GuestId == guestId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PartyMusicResponse(
                x.Id, x.GuestId, guestName, x.Song, x.Artist,
                x.Comment, x.Status, x.CreatedAt, x.SpotifyTrackId, x.SpotifyUri,
                x.SpotifyAlbumImageUrl, x.DurationMs, x.SpotifyQueuedAt,
                dbContext.PartyMusicVotes.Count(vote => vote.PartyMusicRequestId == x.Id),
                dbContext.PartyMusicVotes.Any(vote => vote.PartyMusicRequestId == x.Id && vote.GuestId == guestId)))
            .ToArrayAsync(cancellationToken);
        return Results.Ok(requests);
    }

    private static async Task<IResult> ToggleMusicVoteAsync(
        string slug,
        Guid requestId,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (!await dbContext.PartyMusicRequests.AnyAsync(
            x => x.Id == requestId
                && x.PartyId == access.Party!.Id
                && x.Status == PartyMusicRequestStatus.Open,
            cancellationToken))
        {
            return Results.NotFound();
        }

        var vote = await dbContext.PartyMusicVotes.SingleOrDefaultAsync(
            x => x.PartyMusicRequestId == requestId && x.GuestId == access.Guest!.Id,
            cancellationToken);
        var hasVoted = vote is null;
        if (vote is null)
        {
            dbContext.PartyMusicVotes.Add(new PartyMusicVote
            {
                PartyMusicRequestId = requestId,
                GuestId = access.Guest!.Id,
                CreatedAt = timeProvider.GetUtcNow()
            });
        }
        else
        {
            dbContext.PartyMusicVotes.Remove(vote);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.PartyMusicVotes.CountAsync(
            x => x.PartyMusicRequestId == requestId,
            cancellationToken);
        return Results.Ok(new PartyMusicVoteResponse(count, hasVoted));
    }

    private static async Task<PartyMusicResponse> ToMusicResponseAsync(
        IPartyDbContext dbContext,
        Guid requestId,
        Guid currentGuestId,
        CancellationToken cancellationToken) =>
        await (
            from music in dbContext.PartyMusicRequests.AsNoTracking()
            join guest in dbContext.PartyGuests.AsNoTracking() on music.GuestId equals guest.Id
            where music.Id == requestId
            select new PartyMusicResponse(
                music.Id, music.GuestId, guest.Name, music.Song, music.Artist,
                music.Comment, music.Status, music.CreatedAt, music.SpotifyTrackId,
                music.SpotifyUri, music.SpotifyAlbumImageUrl, music.DurationMs,
                music.SpotifyQueuedAt,
                dbContext.PartyMusicVotes.Count(vote => vote.PartyMusicRequestId == music.Id),
                dbContext.PartyMusicVotes.Any(vote => vote.PartyMusicRequestId == music.Id && vote.GuestId == currentGuestId)))
            .SingleAsync(cancellationToken);

    private static IResult? Denied(Party? party, PartyGuest? guest)
    {
        if (party is null)
        {
            return Results.NotFound();
        }
        if (!party.IsActive)
        {
            return Results.Conflict(new { message = "Diese Party ist aktuell nicht aktiv." });
        }
        return guest is null ? Results.Unauthorized() : null;
    }

    private static string NormalizeFileName(string fileName, string fallbackExtension)
    {
        var normalized = Path.GetFileName(fileName).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return $"upload{fallbackExtension}";
        }
        return normalized[..Math.Min(normalized.Length, 240)];
    }
}
