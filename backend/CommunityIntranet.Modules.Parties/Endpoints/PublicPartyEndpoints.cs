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
        group.MapPost("/guests", RegisterGuestAsync);
        group.MapPatch("/guests/me", UpdateGuestAsync);
        group.MapPost("/orders", CreateOrderAsync);
        group.MapGet("/media", ListMediaAsync);
        group.MapPost("/media", UploadMediaAsync).DisableAntiforgery();
        group.MapGet("/media/{mediaId:guid}/content", GetMediaContentAsync);
        group.MapGet("/guestbook", ListGuestbookAsync);
        group.MapPost("/guestbook", AddGuestbookAsync);
        group.MapPost("/music-requests", AddMusicAsync);
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
            new PartyMediaResponse(media.Id, media.GuestId, access.Guest.Name, media.MediaType, media.FileName, media.MimeType, media.Size, media.Caption, media.CreatedAt, $"/api/parties/public/{slug}/media/{media.Id}/content"));
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

        return Results.Ok(await AdminPartyEndpoints.QueryMedia(dbContext, access.Party.Id, $"/api/parties/public/{slug}/media", cancellationToken));
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
        if (!access.Party!.GuestsCanViewGallery)
        {
            return Results.Forbid();
        }

        var media = await dbContext.PartyMedia.AsNoTracking().SingleOrDefaultAsync(x => x.Id == mediaId && x.PartyId == access.Party.Id, cancellationToken);
        if (media is null)
        {
            return Results.NotFound();
        }
        var stream = await storage.OpenReadAsync(media.StoragePath, cancellationToken);
        return stream is null ? Results.NotFound() : Results.Stream(stream, media.MimeType, enableRangeProcessing: media.MediaType == "video");
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
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = Denied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (string.IsNullOrWhiteSpace(request.Song) || request.Song.Trim().Length > 200)
        {
            return PartyEndpointHelpers.Validation("song", "Bitte gib einen Song mit maximal 200 Zeichen an.");
        }

        var music = new PartyMusicRequest
        {
            Id = Guid.NewGuid(), PartyId = access.Party!.Id, GuestId = access.Guest!.Id,
            Song = request.Song.Trim(), Artist = PartyEndpointHelpers.Clean(request.Artist, 200),
            Comment = PartyEndpointHelpers.Clean(request.Comment, 500),
            Status = PartyMusicRequestStatus.Open, CreatedAt = timeProvider.GetUtcNow()
        };
        dbContext.PartyMusicRequests.Add(music);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/parties/public/{slug}/music-requests/{music.Id}", new PartyMusicResponse(music.Id, music.GuestId, access.Guest.Name, music.Song, music.Artist, music.Comment, music.Status, music.CreatedAt));
    }

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
