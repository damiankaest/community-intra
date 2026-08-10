using System.Security.Claims;
using System.Text.Json;
using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Identity.Persistence;
using CommunityIntranet.Modules.Identity.Security;
using CommunityIntranet.Modules.Identity.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIntranet.Modules.Identity.Endpoints;

internal static class ExternalIdentityEndpoints
{
    private const string RefreshCookieName = "community_refresh";
    private const string ExternalCookieScheme = IdentityConstants.ExternalScheme;
    private static readonly HashSet<string> OAuthProviders = ["Google", "Discord"];

    internal static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Connected Accounts");
        group.MapGet("/providers", GetProvidersAsync);
        group.MapGet("/external/{provider}/start", StartExternalAsync)
            .RequireRateLimiting("authentication");
        group.MapGet("/external/callback", ExternalCallbackAsync)
            .RequireRateLimiting("authentication");
        group.MapGet("/connections", GetConnectionsAsync).RequireAuthorization();
        group.MapPost("/connections/{provider}", CreateConnectionLinkAsync).RequireAuthorization();
        group.MapDelete("/connections/{provider}", DisconnectAsync).RequireAuthorization();
        group.MapGet("/steam/start", StartSteamAsync).RequireRateLimiting("authentication");
        group.MapGet("/steam/callback", SteamCallbackAsync).RequireRateLimiting("authentication");
        return endpoints;
    }

    private static async Task<IResult> GetProvidersAsync(
        IAuthenticationSchemeProvider schemeProvider)
    {
        var google = await schemeProvider.GetSchemeAsync("Google") is not null;
        var discord = await schemeProvider.GetSchemeAsync("Discord") is not null;
        return Results.Ok(new { google, discord, steam = true });
    }

    private static async Task<IResult> StartExternalAsync(
        string provider,
        string? linkToken,
        string? returnUrl,
        IAuthenticationSchemeProvider schemeProvider,
        ExternalLinkTokenService linkTokenService)
    {
        var scheme = NormalizeProvider(provider);
        if (scheme is null || await schemeProvider.GetSchemeAsync(scheme) is null)
        {
            return Results.NotFound(new { message = "Dieser Login-Anbieter ist nicht konfiguriert." });
        }

        Guid? linkUserId = null;
        if (!string.IsNullOrWhiteSpace(linkToken))
        {
            if (!linkTokenService.TryRead(linkToken, out var userId))
            {
                return Results.BadRequest(new { message = "Der Verknüpfungslink ist abgelaufen." });
            }
            linkUserId = userId;
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = "/api/auth/external/callback"
        };
        properties.Items["provider"] = scheme;
        properties.Items["return_url"] = SafeReturnUrl(returnUrl);
        if (linkUserId is not null)
        {
            properties.Items["link_user_id"] = linkUserId.Value.ToString("N");
        }
        return Results.Challenge(properties, [scheme]);
    }

    private static async Task<IResult> ExternalCallbackAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        IIdentityDbContext dbContext,
        TokenService tokenService,
        IWebHostEnvironment environment,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var authentication = await httpContext.AuthenticateAsync(ExternalCookieScheme);
        if (!authentication.Succeeded || authentication.Principal is null)
        {
            return RedirectResult("/login", "external_error");
        }

        var provider = authentication.Properties?.Items.GetValueOrDefault("provider");
        var subject = authentication.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (provider is null || !OAuthProviders.Contains(provider) || string.IsNullOrWhiteSpace(subject))
        {
            await httpContext.SignOutAsync(ExternalCookieScheme);
            return RedirectResult("/login", "external_invalid");
        }

        var returnUrl = SafeReturnUrl(authentication.Properties?.Items.GetValueOrDefault("return_url"));
        ApplicationUser? user;
        if (Guid.TryParseExact(authentication.Properties?.Items.GetValueOrDefault("link_user_id"), "N", out var linkUserId))
        {
            user = await userManager.FindByIdAsync(linkUserId.ToString());
            if (user is null)
            {
                await httpContext.SignOutAsync(ExternalCookieScheme);
                return RedirectResult("/account", "link_expired");
            }
            var linked = await LinkLoginAsync(userManager, user, provider, subject);
            await httpContext.SignOutAsync(ExternalCookieScheme);
            return RedirectResult(returnUrl == "/" ? "/account" : returnUrl, linked ? "linked" : "link_conflict");
        }

        user = await userManager.FindByLoginAsync(provider, subject);
        if (user is null)
        {
            var email = authentication.Principal.FindFirstValue(ClaimTypes.Email);
            var emailVerified = string.Equals(
                authentication.Principal.FindFirstValue("email_verified"),
                "true",
                StringComparison.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(email) || !emailVerified)
            {
                await httpContext.SignOutAsync(ExternalCookieScheme);
                return RedirectResult("/login", "verified_email_required");
            }
            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    DisplayName = authentication.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0],
                    AvatarUrl = authentication.Principal.FindFirstValue("picture"),
                    CreatedAt = timeProvider.GetUtcNow(),
                    LastLoginAt = timeProvider.GetUtcNow(),
                    IsActive = true
                };
                var created = await userManager.CreateAsync(user);
                if (!created.Succeeded)
                {
                    await httpContext.SignOutAsync(ExternalCookieScheme);
                    return RedirectResult("/login", "external_create_failed");
                }
            }
            else
            {
                user.EmailConfirmed = true;
            }

            if (!await LinkLoginAsync(userManager, user, provider, subject))
            {
                await httpContext.SignOutAsync(ExternalCookieScheme);
                return RedirectResult("/login", "external_conflict");
            }
        }

        if (!user.IsActive)
        {
            await httpContext.SignOutAsync(ExternalCookieScheme);
            return RedirectResult("/login", "account_disabled");
        }
        user.LastLoginAt = timeProvider.GetUtcNow();
        await userManager.UpdateAsync(user);
        await CreateSessionAsync(user, dbContext, tokenService, environment, httpContext, cancellationToken);
        await httpContext.SignOutAsync(ExternalCookieScheme);
        return RedirectResult(returnUrl, "success");
    }

    private static async Task<IResult> GetConnectionsAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IIdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Unauthorized();
        }
        var logins = await userManager.GetLoginsAsync(user);
        var steam = await dbContext.SteamIdentities.AsNoTracking()
            .SingleOrDefaultAsync(identity => identity.UserId == user.Id, cancellationToken);
        return Results.Ok(new
        {
            google = Connection(logins, "Google"),
            discord = Connection(logins, "Discord"),
            steam = steam is null
                ? new { connected = false, displayName = (string?)null, avatarUrl = (string?)null, steamId64 = (string?)null, linkedAt = (DateTimeOffset?)null }
                : new { connected = true, displayName = (string?)steam.DisplayName, avatarUrl = steam.AvatarUrl, steamId64 = (string?)steam.SteamId64, linkedAt = (DateTimeOffset?)steam.LinkedAt }
        });
    }

    private static async Task<IResult> CreateConnectionLinkAsync(
        string provider,
        string? returnUrl,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IAuthenticationSchemeProvider schemeProvider,
        ExternalLinkTokenService linkTokenService)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Unauthorized();
        }
        var scheme = NormalizeProvider(provider);
        if (scheme == "Steam")
        {
            var token = Uri.EscapeDataString(linkTokenService.Create(user.Id));
            return Results.Ok(new { url = $"/api/auth/steam/start?linkToken={token}&returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}" });
        }
        if (scheme is null || await schemeProvider.GetSchemeAsync(scheme) is null)
        {
            return Results.NotFound(new { message = "Dieser Anbieter ist nicht konfiguriert." });
        }
        var linkToken = Uri.EscapeDataString(linkTokenService.Create(user.Id));
        return Results.Ok(new { url = $"/api/auth/external/{scheme}/start?linkToken={linkToken}&returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl))}" });
    }

    private static async Task<IResult> DisconnectAsync(
        string provider,
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager,
        IIdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return Results.Unauthorized();
        }
        var scheme = NormalizeProvider(provider);
        if (scheme == "Steam")
        {
            var steam = await dbContext.SteamIdentities.SingleOrDefaultAsync(identity => identity.UserId == user.Id, cancellationToken);
            if (steam is not null)
            {
                dbContext.SteamIdentities.Remove(steam);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return Results.NoContent();
        }
        if (scheme is null)
        {
            return Results.NotFound();
        }
        var logins = await userManager.GetLoginsAsync(user);
        var login = logins.SingleOrDefault(item => item.LoginProvider == scheme);
        if (login is null)
        {
            return Results.NoContent();
        }
        if (!await userManager.HasPasswordAsync(user) && logins.Count <= 1)
        {
            return Results.Conflict(new { message = "Verbinde zuerst eine andere Anmeldemethode, bevor du diese trennst." });
        }
        var result = await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        return result.Succeeded ? Results.NoContent() : Results.Conflict(new { message = "Die Verbindung konnte nicht getrennt werden." });
    }

    private static IResult StartSteamAsync(
        string linkToken,
        string? returnUrl,
        HttpContext httpContext,
        ExternalLinkTokenService linkTokenService)
    {
        if (!linkTokenService.TryRead(linkToken, out _))
        {
            return Results.BadRequest(new { message = "Der Steam-Verknüpfungslink ist abgelaufen." });
        }
        var callback = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/auth/steam/callback";
        callback = QueryHelpers.AddQueryString(callback, new Dictionary<string, string?>
        {
            ["linkToken"] = linkToken,
            ["returnUrl"] = SafeReturnUrl(returnUrl)
        });
        var realm = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/";
        var url = QueryHelpers.AddQueryString("https://steamcommunity.com/openid/login", new Dictionary<string, string?>
        {
            ["openid.ns"] = "http://specs.openid.net/auth/2.0",
            ["openid.mode"] = "checkid_setup",
            ["openid.return_to"] = callback,
            ["openid.realm"] = realm,
            ["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select",
            ["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select"
        });
        return Results.Redirect(url);
    }

    private static async Task<IResult> SteamCallbackAsync(
        string linkToken,
        string? returnUrl,
        HttpContext httpContext,
        ExternalLinkTokenService linkTokenService,
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalLoginOptions> externalOptions,
        IIdentityDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!linkTokenService.TryRead(linkToken, out var userId))
        {
            return RedirectResult("/account", "steam_link_expired");
        }
        if (!string.Equals(
                httpContext.Request.Query["openid.op_endpoint"].ToString(),
                "https://steamcommunity.com/openid/login",
                StringComparison.Ordinal)
            || !string.Equals(httpContext.Request.Query["openid.mode"].ToString(), "id_res", StringComparison.Ordinal))
        {
            return RedirectResult("/account", "steam_invalid");
        }
        var values = httpContext.Request.Query
            .Where(item => item.Key.StartsWith("openid.", StringComparison.Ordinal))
            .ToDictionary(item => item.Key, item => item.Value.ToString());
        values["openid.mode"] = "check_authentication";
        var client = httpClientFactory.CreateClient("SteamIdentity");
        using var verificationResponse = await client.PostAsync(
            "https://steamcommunity.com/openid/login",
            new FormUrlEncodedContent(values),
            cancellationToken);
        var verification = await verificationResponse.Content.ReadAsStringAsync(cancellationToken);
        var isValid = verification.Split('\n', StringSplitOptions.TrimEntries)
            .Any(line => string.Equals(line, "is_valid:true", StringComparison.Ordinal));
        if (!verificationResponse.IsSuccessStatusCode || !isValid)
        {
            return RedirectResult("/account", "steam_invalid");
        }
        var claimedId = httpContext.Request.Query["openid.claimed_id"].ToString();
        const string prefix = "https://steamcommunity.com/openid/id/";
        if (!claimedId.StartsWith(prefix, StringComparison.Ordinal)
            || !ulong.TryParse(claimedId[prefix.Length..], out var steamId))
        {
            return RedirectResult("/account", "steam_invalid");
        }
        var steamId64 = steamId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!await dbContext.Users.AsNoTracking()
                .AnyAsync(user => user.Id == userId && user.IsActive, cancellationToken))
        {
            return RedirectResult("/account", "steam_link_expired");
        }
        var conflict = await dbContext.SteamIdentities.AsNoTracking()
            .AnyAsync(identity => identity.SteamId64 == steamId64 && identity.UserId != userId, cancellationToken);
        if (conflict)
        {
            return RedirectResult("/account", "steam_conflict");
        }
        var profile = await GetSteamProfileAsync(client, externalOptions.Value.SteamApiKey, steamId64, cancellationToken);
        var identity = await dbContext.SteamIdentities.SingleOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (identity is null)
        {
            identity = new SteamIdentity { Id = Guid.NewGuid(), UserId = userId, SteamId64 = steamId64, DisplayName = profile.Name, AvatarUrl = profile.AvatarUrl, LinkedAt = timeProvider.GetUtcNow(), ProfileUpdatedAt = timeProvider.GetUtcNow() };
            dbContext.SteamIdentities.Add(identity);
        }
        else
        {
            identity.SteamId64 = steamId64;
            identity.DisplayName = profile.Name;
            identity.AvatarUrl = profile.AvatarUrl;
            identity.LinkedAt = timeProvider.GetUtcNow();
            identity.ProfileUpdatedAt = timeProvider.GetUtcNow();
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return RedirectResult(SafeReturnUrl(returnUrl) == "/" ? "/account" : SafeReturnUrl(returnUrl), "steam_linked");
    }

    private static async Task<(string Name, string? AvatarUrl)> GetSteamProfileAsync(
        HttpClient client, string apiKey, string steamId64, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return ($"Steam {SteamSuffix(steamId64)}", null);
        }
        var url = QueryHelpers.AddQueryString("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/", new Dictionary<string, string?>
        {
            ["key"] = apiKey,
            ["steamids"] = steamId64
        });
        using var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return ($"Steam {SteamSuffix(steamId64)}", null);
        }
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var players = json.RootElement.GetProperty("response").GetProperty("players");
        if (players.GetArrayLength() == 0)
        {
            return ($"Steam {SteamSuffix(steamId64)}", null);
        }
        var player = players[0];
        return (
            player.GetProperty("personaname").GetString() ?? $"Steam {SteamSuffix(steamId64)}",
            player.TryGetProperty("avatarfull", out var avatar) ? avatar.GetString() : null);
    }

    private static async Task<bool> LinkLoginAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string provider, string subject)
    {
        var currentOwner = await userManager.FindByLoginAsync(provider, subject);
        if (currentOwner is not null)
        {
            return currentOwner.Id == user.Id;
        }
        var result = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, subject, provider));
        return result.Succeeded;
    }

    private static async Task CreateSessionAsync(
        ApplicationUser user,
        IIdentityDbContext dbContext,
        TokenService tokenService,
        IWebHostEnvironment environment,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var refreshToken = tokenService.CreateRefreshToken(user.Id, Guid.NewGuid(), GetClientIp(httpContext));
        dbContext.RefreshTokens.Add(refreshToken.Token);
        await dbContext.SaveChangesAsync(cancellationToken);
        httpContext.Response.Cookies.Append(RefreshCookieName, refreshToken.RawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = refreshToken.Token.ExpiresAt,
            IsEssential = true
        });
    }

    private static object Connection(IList<UserLoginInfo> logins, string provider)
    {
        var login = logins.SingleOrDefault(item => item.LoginProvider == provider);
        return new { connected = login is not null, displayName = login?.ProviderDisplayName };
    }

    private static string? NormalizeProvider(string value) => value.ToLowerInvariant() switch
    {
        "google" => "Google",
        "discord" => "Discord",
        "steam" => "Steam",
        _ => null
    };

    private static string SafeReturnUrl(string? value) =>
        !string.IsNullOrWhiteSpace(value)
            && value.StartsWith('/')
            && !value.StartsWith("//", StringComparison.Ordinal)
            && !value.StartsWith("/\\", StringComparison.Ordinal)
            && !value.Contains('\r')
            && !value.Contains('\n')
            ? value
            : "/organizations";

    private static IResult RedirectResult(string path, string status) =>
        Results.Redirect(QueryHelpers.AddQueryString(path, "auth", status));

    private static string GetClientIp(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static string SteamSuffix(string steamId64) =>
        steamId64.Length <= 4 ? steamId64 : steamId64[^4..];
}
