using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommunityIntranet.Modules.Parties.Contracts;
using CommunityIntranet.Modules.Parties.Domain;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.Parties.Services;

public sealed class PartySpotifyOptions
{
    public const string SectionName = "Spotify";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public string RedirectUri { get; init; } = string.Empty;
}

public sealed record PartySpotifyTokenResult(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn);

public sealed record PartySpotifyOAuthState(Guid PartyId, Guid OwnerUserId);

public interface IPartySpotifyTokenProtector
{
    string ProtectRefreshToken(Guid partyId, string refreshToken);
    string UnprotectRefreshToken(Guid partyId, string protectedRefreshToken);
    string ProtectState(Guid partyId, Guid ownerUserId, DateTimeOffset expiresAt);
    bool TryUnprotectState(string protectedState, DateTimeOffset now, out PartySpotifyOAuthState state);
}

public sealed class PartySpotifyTokenProtector(IDataProtectionProvider provider)
    : IPartySpotifyTokenProtector
{
    private readonly IDataProtector refreshProtector = provider.CreateProtector(
        "CommunityIntranet.Parties.Spotify.RefreshToken.v1");
    private readonly IDataProtector stateProtector = provider.CreateProtector(
        "CommunityIntranet.Parties.Spotify.OAuthState.v1");

    public string ProtectRefreshToken(Guid partyId, string refreshToken) =>
        refreshProtector.Protect($"{partyId:N}:{refreshToken}");

    public string UnprotectRefreshToken(Guid partyId, string protectedRefreshToken)
    {
        var value = refreshProtector.Unprotect(protectedRefreshToken);
        var prefix = $"{partyId:N}:";
        if (!value.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Spotify token belongs to another party.");
        }

        return value[prefix.Length..];
    }

    public string ProtectState(Guid partyId, Guid ownerUserId, DateTimeOffset expiresAt) =>
        stateProtector.Protect($"{partyId:N}|{ownerUserId:N}|{expiresAt.ToUnixTimeSeconds()}");

    public bool TryUnprotectState(
        string protectedState,
        DateTimeOffset now,
        out PartySpotifyOAuthState state)
    {
        state = default!;
        try
        {
            var parts = stateProtector.Unprotect(protectedState).Split('|');
            if (parts.Length != 3
                || !Guid.TryParseExact(parts[0], "N", out var partyId)
                || !Guid.TryParseExact(parts[1], "N", out var ownerUserId)
                || !long.TryParse(parts[2], out var expiresUnix)
                || now.ToUnixTimeSeconds() > expiresUnix)
            {
                return false;
            }

            state = new PartySpotifyOAuthState(partyId, ownerUserId);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public interface IPartySpotifyClient
{
    bool IsConfigured { get; }
    string CreateAuthorizeUrl(string redirectUri, string state);
    Task<PartySpotifyTokenResult> ExchangeCodeAsync(string code, string redirectUri, CancellationToken cancellationToken);
    Task<string?> GetProfileNameAsync(string accessToken, CancellationToken cancellationToken);
    Task<IReadOnlyList<SpotifyTrackResponse>> SearchTracksAsync(Party party, string query, CancellationToken cancellationToken);
    Task<SpotifyTrackResponse?> GetTrackAsync(Party party, string trackId, CancellationToken cancellationToken);
    Task<SpotifyNowPlayingResponse?> GetNowPlayingAsync(Party party, CancellationToken cancellationToken);
    Task AddToQueueAsync(Party party, string spotifyUri, CancellationToken cancellationToken);
    void CacheAccessToken(Guid partyId, string accessToken, int expiresIn);
    void ClearAccessToken(Guid partyId);
}

public sealed class PartySpotifyClient(
    HttpClient httpClient,
    IOptions<PartySpotifyOptions> options,
    IPartySpotifyTokenProtector tokenProtector,
    IMemoryCache cache) : IPartySpotifyClient
{
    private static readonly string[] Scopes =
    [
        "user-read-private",
        "user-read-playback-state",
        "user-read-currently-playing",
        "user-modify-playback-state"
    ];

    private readonly PartySpotifyOptions spotifyOptions = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(spotifyOptions.ClientId)
        && !string.IsNullOrWhiteSpace(spotifyOptions.ClientSecret);

    public string CreateAuthorizeUrl(string redirectUri, string state)
    {
        EnsureConfigured();
        return "https://accounts.spotify.com/authorize"
            + $"?client_id={Uri.EscapeDataString(spotifyOptions.ClientId)}"
            + "&response_type=code"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&state={Uri.EscapeDataString(state)}"
            + $"&scope={Uri.EscapeDataString(string.Join(' ', Scopes))}"
            + "&show_dialog=true";
    }

    public async Task<PartySpotifyTokenResult> ExchangeCodeAsync(
        string code,
        string redirectUri,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        using var request = CreateTokenRequest(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri
        });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        using var json = await ReadSpotifyJsonAsync(response, cancellationToken);
        var root = json.RootElement;
        var refreshToken = root.GetProperty("refresh_token").GetString();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new PartySpotifyException("Spotify hat kein Refresh-Token geliefert.");
        }

        return new PartySpotifyTokenResult(
            root.GetProperty("access_token").GetString()!,
            refreshToken,
            root.TryGetProperty("expires_in", out var expires) ? expires.GetInt32() : 3600);
    }

    public async Task<string?> GetProfileNameAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = CreateApiRequest(HttpMethod.Get, "me", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        using var json = await ReadSpotifyJsonAsync(response, cancellationToken);
        var root = json.RootElement;
        if (root.TryGetProperty("display_name", out var displayName)
            && !string.IsNullOrWhiteSpace(displayName.GetString()))
        {
            return displayName.GetString();
        }
        return root.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    public async Task<IReadOnlyList<SpotifyTrackResponse>> SearchTracksAsync(
        Party party,
        string query,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(party, cancellationToken);
        var path = $"search?q={Uri.EscapeDataString(query)}&type=track&limit=8";
        using var request = CreateApiRequest(HttpMethod.Get, path, accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        using var json = await ReadSpotifyJsonAsync(response, cancellationToken);
        if (!json.RootElement.TryGetProperty("tracks", out var tracks)
            || !tracks.TryGetProperty("items", out var items))
        {
            return [];
        }

        var result = new List<SpotifyTrackResponse>();
        foreach (var item in items.EnumerateArray())
        {
            var track = ParseTrack(item);
            if (track is not null)
            {
                result.Add(track);
            }
        }
        return result;
    }

    public async Task<SpotifyTrackResponse?> GetTrackAsync(
        Party party,
        string trackId,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(party, cancellationToken);
        using var request = CreateApiRequest(
            HttpMethod.Get,
            $"tracks/{Uri.EscapeDataString(trackId)}",
            accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        using var json = await ReadSpotifyJsonAsync(response, cancellationToken);
        return ParseTrack(json.RootElement);
    }

    public async Task<SpotifyNowPlayingResponse?> GetNowPlayingAsync(
        Party party,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(party, cancellationToken);
        using var request = CreateApiRequest(
            HttpMethod.Get,
            "me/player/currently-playing",
            accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        using var json = await ReadSpotifyJsonAsync(response, cancellationToken);
        var root = json.RootElement;
        if (!root.TryGetProperty("item", out var item) || item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        var track = ParseTrack(item);
        if (track is null)
        {
            return null;
        }

        return new SpotifyNowPlayingResponse(
            root.TryGetProperty("is_playing", out var playing) && playing.GetBoolean(),
            track.Id,
            track.Uri,
            track.Name,
            track.Artist,
            track.AlbumImageUrl,
            track.DurationMs,
            root.TryGetProperty("progress_ms", out var progress) && progress.ValueKind == JsonValueKind.Number
                ? progress.GetInt32()
                : 0);
    }

    public async Task AddToQueueAsync(
        Party party,
        string spotifyUri,
        CancellationToken cancellationToken)
    {
        var accessToken = await GetAccessTokenAsync(party, cancellationToken);
        using var request = CreateApiRequest(
            HttpMethod.Post,
            $"me/player/queue?uri={Uri.EscapeDataString(spotifyUri)}",
            accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await ThrowSpotifyErrorAsync(response, cancellationToken);
        }
    }

    public void CacheAccessToken(Guid partyId, string accessToken, int expiresIn) =>
        cache.Set(
            AccessTokenCacheKey(partyId),
            accessToken,
            TimeSpan.FromSeconds(Math.Max(30, expiresIn - 60)));

    public void ClearAccessToken(Guid partyId) => cache.Remove(AccessTokenCacheKey(partyId));

    private async Task<string> GetAccessTokenAsync(
        Party party,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue<string>(AccessTokenCacheKey(party.Id), out var cached)
            && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }
        if (string.IsNullOrWhiteSpace(party.SpotifyProtectedRefreshToken))
        {
            throw new PartySpotifyException("Spotify ist für diese Party nicht verbunden.");
        }

        EnsureConfigured();
        var refreshToken = tokenProtector.UnprotectRefreshToken(
            party.Id,
            party.SpotifyProtectedRefreshToken);
        using var request = CreateTokenRequest(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        });
        using var response = await httpClient.SendAsync(request, cancellationToken);
        using var json = await ReadSpotifyJsonAsync(response, cancellationToken);
        var root = json.RootElement;
        var accessToken = root.GetProperty("access_token").GetString()!;
        var expiresIn = root.TryGetProperty("expires_in", out var expires) ? expires.GetInt32() : 3600;
        CacheAccessToken(party.Id, accessToken, expiresIn);
        return accessToken;
    }

    private HttpRequestMessage CreateTokenRequest(Dictionary<string, string> form)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://accounts.spotify.com/api/token")
        {
            Content = new FormUrlEncodedContent(form)
        };
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{spotifyOptions.ClientId}:{spotifyOptions.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        return request;
    }

    private static HttpRequestMessage CreateApiRequest(
        HttpMethod method,
        string path,
        string accessToken)
    {
        var request = new HttpRequestMessage(method, $"https://api.spotify.com/v1/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    private static SpotifyTrackResponse? ParseTrack(JsonElement item)
    {
        if (!item.TryGetProperty("id", out var idElement)
            || !item.TryGetProperty("uri", out var uriElement)
            || !item.TryGetProperty("name", out var nameElement))
        {
            return null;
        }
        var id = idElement.GetString();
        var uri = uriElement.GetString();
        var name = nameElement.GetString();
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(uri)
            || string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var artists = item.TryGetProperty("artists", out var artistsElement)
            ? artistsElement.EnumerateArray()
                .Select(artist => artist.TryGetProperty("name", out var artistName) ? artistName.GetString() : null)
                .Where(artist => !string.IsNullOrWhiteSpace(artist))
                .ToArray()
            : [];
        string? albumImage = null;
        if (item.TryGetProperty("album", out var album)
            && album.TryGetProperty("images", out var images))
        {
            albumImage = images.EnumerateArray()
                .Select(image => image.TryGetProperty("url", out var url) ? url.GetString() : null)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        }
        var durationMs = item.TryGetProperty("duration_ms", out var duration)
            && duration.ValueKind == JsonValueKind.Number
            ? duration.GetInt32()
            : 0;
        return new SpotifyTrackResponse(
            id,
            uri,
            name,
            string.Join(", ", artists!),
            albumImage,
            durationMs);
    }

    private static async Task<JsonDocument> ReadSpotifyJsonAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            await ThrowSpotifyErrorAsync(response, cancellationToken);
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task ThrowSpotifyErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new PartySpotifyException(
            $"Spotify API ({(int)response.StatusCode}) ist gerade nicht verfügbar. {detail[..Math.Min(detail.Length, 240)]}");
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new PartySpotifyException(
                "Spotify ist auf dem Server noch nicht konfiguriert.");
        }
    }

    private static string AccessTokenCacheKey(Guid partyId) => $"party-spotify:{partyId:N}";
}

public sealed class PartySpotifyException : Exception
{
    public PartySpotifyException()
    {
    }

    public PartySpotifyException(string message) : base(message)
    {
    }

    public PartySpotifyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
