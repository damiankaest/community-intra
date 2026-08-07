using System.Security.Claims;
using CommunityIntranet.Modules.Parties.Contracts;
using CommunityIntranet.Modules.Parties.Domain;
using CommunityIntranet.Modules.Parties.Persistence;
using CommunityIntranet.Modules.Parties.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.Parties.Endpoints;

internal static class SpotifyPartyEndpoints
{
    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/parties/spotify/callback", CallbackAsync)
            .WithTags("Parties");

        var admin = endpoints.MapGroup("/api/parties/{partyId:guid}/spotify")
            .WithTags("Parties")
            .RequireAuthorization();
        admin.MapGet("/", GetAdminStatusAsync);
        admin.MapPost("/connect", ConnectAsync);
        admin.MapPost("/disconnect", DisconnectAsync);
        admin.MapPatch("/", UpdateAsync);
        admin.MapPost("/queue/{requestId:guid}", QueueRequestAsync);

        var guest = endpoints.MapGroup("/api/parties/public/{slug}/spotify")
            .WithTags("Party Guest")
            .RequireRateLimiting("party-public");
        guest.MapGet("/", GetPublicStatusAsync);
        guest.MapGet("/search", SearchAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAdminStatusAsync(
        Guid partyId,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        CancellationToken cancellationToken)
    {
        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(
            dbContext, partyId, principal, cancellationToken);
        if (party is null)
        {
            return Results.NotFound();
        }

        var connected = !string.IsNullOrWhiteSpace(party.SpotifyProtectedRefreshToken);
        return Results.Ok(new PartySpotifyAdminStatusResponse(
            spotify.IsConfigured,
            connected,
            party.SpotifyAccountName,
            party.SpotifyAutoQueue,
            connected ? await TryGetNowPlayingAsync(spotify, party, cancellationToken) : null));
    }

    private static async Task<IResult> ConnectAsync(
        Guid partyId,
        HttpRequest request,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        IPartySpotifyTokenProtector tokenProtector,
        IOptions<PartySpotifyOptions> options,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(
            dbContext, partyId, principal, cancellationToken);
        if (party is null)
        {
            return Results.NotFound();
        }
        if (!spotify.IsConfigured)
        {
            return Results.Problem(
                "Spotify Client-ID und Client-Secret fehlen auf dem Server.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var state = tokenProtector.ProtectState(
            party.Id,
            party.OwnerUserId,
            timeProvider.GetUtcNow().AddMinutes(10));
        var redirectUri = GetRedirectUri(request, options.Value);
        return Results.Ok(new PartySpotifyConnectResponse(
            spotify.CreateAuthorizeUrl(redirectUri, state)));
    }

    private static async Task<IResult> CallbackAsync(
        string? code,
        string? state,
        string? error,
        HttpRequest request,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        IPartySpotifyTokenProtector tokenProtector,
        IOptions<PartySpotifyOptions> options,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state)
            || !tokenProtector.TryUnprotectState(
                state,
                timeProvider.GetUtcNow(),
                out var oauthState))
        {
            return Results.Redirect("/parties?spotify=invalid-state");
        }

        var party = await dbContext.Parties.SingleOrDefaultAsync(
            x => x.Id == oauthState.PartyId
                && x.OwnerUserId == oauthState.OwnerUserId
                && !x.IsArchived,
            cancellationToken);
        if (party is null)
        {
            return Results.Redirect("/parties?spotify=party-not-found");
        }

        var adminUrl = $"/parties/{party.Id}";
        if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(code))
        {
            return Results.Redirect($"{adminUrl}?spotify=denied");
        }

        try
        {
            var redirectUri = GetRedirectUri(request, options.Value);
            var token = await spotify.ExchangeCodeAsync(code, redirectUri, cancellationToken);
            var accountName = await spotify.GetProfileNameAsync(token.AccessToken, cancellationToken);
            party.SpotifyProtectedRefreshToken = tokenProtector.ProtectRefreshToken(
                party.Id,
                token.RefreshToken);
            party.SpotifyAccountName = PartyEndpointHelpers.Clean(accountName, 200);
            party.SpotifyConnectedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            spotify.CacheAccessToken(party.Id, token.AccessToken, token.ExpiresIn);
            return Results.Redirect($"{adminUrl}?spotify=connected");
        }
        catch (PartySpotifyException)
        {
            return Results.Redirect($"{adminUrl}?spotify=error");
        }
    }

    private static async Task<IResult> DisconnectAsync(
        Guid partyId,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        CancellationToken cancellationToken)
    {
        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(
            dbContext, partyId, principal, cancellationToken, tracked: true);
        if (party is null)
        {
            return Results.NotFound();
        }

        party.SpotifyProtectedRefreshToken = null;
        party.SpotifyAccountName = null;
        party.SpotifyConnectedAt = null;
        party.SpotifyAutoQueue = false;
        await dbContext.SaveChangesAsync(cancellationToken);
        spotify.ClearAccessToken(party.Id);
        return Results.NoContent();
    }

    private static async Task<IResult> UpdateAsync(
        Guid partyId,
        UpdatePartySpotifyRequest request,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(
            dbContext, partyId, principal, cancellationToken, tracked: true);
        if (party is null)
        {
            return Results.NotFound();
        }
        if (request.AutoQueue && string.IsNullOrWhiteSpace(party.SpotifyProtectedRefreshToken))
        {
            return Results.Conflict(new { message = "Bitte verbinde zuerst Spotify." });
        }

        party.SpotifyAutoQueue = request.AutoQueue;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> QueueRequestAsync(
        Guid partyId,
        Guid requestId,
        ClaimsPrincipal principal,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var party = await PartyEndpointHelpers.GetOwnedPartyAsync(
            dbContext, partyId, principal, cancellationToken, tracked: true);
        if (party is null)
        {
            return Results.NotFound();
        }
        var music = await dbContext.PartyMusicRequests.SingleOrDefaultAsync(
            x => x.Id == requestId && x.PartyId == party.Id,
            cancellationToken);
        if (music is null)
        {
            return Results.NotFound();
        }
        if (string.IsNullOrWhiteSpace(music.SpotifyUri))
        {
            return PartyEndpointHelpers.Validation(
                "spotify",
                "Dieser Wunsch wurde nicht über Spotify ausgewählt.");
        }

        try
        {
            await spotify.AddToQueueAsync(party, music.SpotifyUri, cancellationToken);
            music.SpotifyQueuedAt = timeProvider.GetUtcNow();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }
        catch (PartySpotifyException exception)
        {
            return Results.Problem(
                exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> GetPublicStatusAsync(
        string slug,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(
            dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = GuestDenied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }

        var connected = !string.IsNullOrWhiteSpace(access.Party!.SpotifyProtectedRefreshToken);
        return Results.Ok(new PartySpotifyPublicStatusResponse(
            connected,
            access.Party.SpotifyAutoQueue,
            connected ? await TryGetNowPlayingAsync(spotify, access.Party, cancellationToken) : null));
    }

    private static async Task<IResult> SearchAsync(
        string slug,
        string? q,
        HttpContext httpContext,
        IPartyDbContext dbContext,
        IPartySpotifyClient spotify,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await PartyEndpointHelpers.GetGuestAccessAsync(
            dbContext, slug, httpContext, timeProvider, cancellationToken);
        var denied = GuestDenied(access.Party, access.Guest);
        if (denied is not null)
        {
            return denied;
        }
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length is < 2 or > 80)
        {
            return PartyEndpointHelpers.Validation("q", "Bitte gib 2 bis 80 Zeichen ein.");
        }
        if (string.IsNullOrWhiteSpace(access.Party!.SpotifyProtectedRefreshToken))
        {
            return Results.Conflict(new { message = "Spotify ist für diese Party nicht verbunden." });
        }

        try
        {
            return Results.Ok(await spotify.SearchTracksAsync(
                access.Party,
                q.Trim(),
                cancellationToken));
        }
        catch (PartySpotifyException exception)
        {
            return Results.Problem(
                exception.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<SpotifyNowPlayingResponse?> TryGetNowPlayingAsync(
        IPartySpotifyClient spotify,
        Party party,
        CancellationToken cancellationToken)
    {
        try
        {
            return await spotify.GetNowPlayingAsync(party, cancellationToken);
        }
        catch (PartySpotifyException)
        {
            return null;
        }
    }

    private static string GetRedirectUri(HttpRequest request, PartySpotifyOptions options) =>
        !string.IsNullOrWhiteSpace(options.RedirectUri)
            ? options.RedirectUri
            : $"{request.Scheme}://{request.Host}/api/parties/spotify/callback";

    private static IResult? GuestDenied(Party? party, PartyGuest? guest)
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
}
