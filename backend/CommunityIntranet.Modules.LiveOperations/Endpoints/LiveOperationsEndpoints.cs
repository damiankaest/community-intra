using System.Security.Claims;
using CommunityIntranet.BuildingBlocks.ActivityFeed;
using CommunityIntranet.BuildingBlocks.Authorization;
using CommunityIntranet.BuildingBlocks.LiveOperations;
using CommunityIntranet.BuildingBlocks.Tenancy;
using CommunityIntranet.Modules.LiveOperations.Contracts;
using CommunityIntranet.Modules.LiveOperations.Domain;
using CommunityIntranet.Modules.LiveOperations.Persistence;
using CommunityIntranet.Modules.LiveOperations.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.LiveOperations.Endpoints;

public static class LiveOperationsEndpoints
{
    public static IEndpointRouteBuilder MapLiveOperationsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/organizations/{organizationId:guid}/live-operations/server")
            .WithTags("Live Operations")
            .RequireAuthorization();

        group.MapGet("/status", GetStatusAsync);
        group.MapGet("/configuration", GetConfigurationAsync);
        group.MapPut("/configuration", SaveConfigurationAsync);
        group.MapDelete("/configuration", DeleteConfigurationAsync);
        group.MapPost("/test", TestConnectionAsync);
        return endpoints;
    }

    private static async Task<IResult> GetStatusAsync(
        Guid organizationId,
        bool? forceRefresh,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        ILiveOperationsReader reader,
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

        var status = await reader.GetServerStatusAsync(
            organizationId,
            forceRefresh == true
                && access.Membership!.PermissionRole.CanManageOrganization(),
            cancellationToken);
        return Results.Ok(ToResponse(status));
    }

    private static async Task<IResult> GetConfigurationAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ILiveOperationsDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetManagerAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var connection = await dbContext.GameServerConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.OrganizationId == organizationId,
                cancellationToken);
        return connection is null
            ? Results.NoContent()
            : Results.Ok(ToResponse(connection));
    }

    private static async Task<IResult> SaveConfigurationAsync(
        Guid organizationId,
        SaveGameServerConfigurationRequest request,
        ClaimsPrincipal principal,
        ILiveOperationsDbContext dbContext,
        IOrganizationAccessService accessService,
        IGameServerTokenProtector tokenProtector,
        IActivityWriter activityWriter,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await GetManagerAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var validation = Validate(
            request.DisplayName,
            request.Host,
            request.Port,
            request.ApiToken,
            request.CertificateFingerprint,
            tokenRequired: false);
        if (validation is not null)
        {
            return validation;
        }

        var connection = await dbContext.GameServerConnections.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId,
            cancellationToken);
        var isNew = connection is null;
        if (isNew && string.IsNullOrWhiteSpace(request.ApiToken))
        {
            return Validation(
                "ApiToken",
                "Für eine neue Verbindung wird ein API-Token benötigt.");
        }

        if (connection is not null
            && request.ConcurrencyToken != connection.ConcurrencyToken)
        {
            return Results.Conflict(new
            {
                title = "Server configuration was changed",
                detail = "Reload the configuration before saving again."
            });
        }

        var now = timeProvider.GetUtcNow();
        if (connection is null)
        {
            connection = new GameServerConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                DisplayName = request.DisplayName!.Trim(),
                Host = request.Host!.Trim().ToLowerInvariant(),
                Port = request.Port,
                ProtectedApiToken = tokenProtector.Protect(
                    organizationId,
                    request.ApiToken!.Trim()),
                CertificateFingerprint = NormalizeFingerprint(
                    request.CertificateFingerprint),
                IsEnabled = request.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now,
                ConcurrencyToken = Guid.NewGuid()
            };
            dbContext.GameServerConnections.Add(connection);
        }
        else
        {
            connection.DisplayName = request.DisplayName!.Trim();
            connection.Host = request.Host!.Trim().ToLowerInvariant();
            connection.Port = request.Port;
            if (!string.IsNullOrWhiteSpace(request.ApiToken))
            {
                connection.ProtectedApiToken = tokenProtector.Protect(
                    organizationId,
                    request.ApiToken.Trim());
            }

            connection.CertificateFingerprint = NormalizeFingerprint(
                request.CertificateFingerprint);
            connection.IsEnabled = request.IsEnabled;
            connection.UpdatedAt = now;
            connection.ConcurrencyToken = Guid.NewGuid();
        }

        activityWriter.Add(new ActivityDraft(
            organizationId,
            isNew
                ? "live_operations.server_connected"
                : "live_operations.server_configuration_updated",
            access.Membership!.MemberId,
            "game_server_connection",
            connection.Id,
            new Dictionary<string, string?>
            {
                ["displayName"] = connection.DisplayName
            }));
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(ToResponse(connection));
    }

    private static async Task<IResult> TestConnectionAsync(
        Guid organizationId,
        TestGameServerConnectionRequest request,
        ClaimsPrincipal principal,
        IOrganizationAccessService accessService,
        ILiveOperationsDbContext dbContext,
        IGameServerTokenProtector tokenProtector,
        ISatisfactoryServerClient serverClient,
        CancellationToken cancellationToken)
    {
        var access = await GetManagerAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var validation = Validate(
            request.DisplayName,
            request.Host,
            request.Port,
            request.ApiToken,
            request.CertificateFingerprint,
            tokenRequired: false);
        if (validation is not null)
        {
            return validation;
        }

        var apiToken = request.ApiToken?.Trim();
        if (string.IsNullOrWhiteSpace(apiToken))
        {
            var existing = await dbContext.GameServerConnections
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.OrganizationId == organizationId,
                    cancellationToken);
            if (existing is not null)
            {
                try
                {
                    apiToken = tokenProtector.Unprotect(
                        organizationId,
                        existing.ProtectedApiToken);
                }
                catch (System.Security.Cryptography.CryptographicException)
                {
                    apiToken = null;
                }
                catch (InvalidOperationException)
                {
                    apiToken = null;
                }
            }
        }

        var status = await serverClient.ProbeAsync(
            new SatisfactoryServerTarget(
                request.DisplayName!.Trim(),
                request.Host!.Trim().ToLowerInvariant(),
                request.Port,
                apiToken ?? string.Empty,
                NormalizeFingerprint(request.CertificateFingerprint)),
            cancellationToken);
        return Results.Ok(ToResponse(status));
    }

    private static async Task<IResult> DeleteConfigurationAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
        ILiveOperationsDbContext dbContext,
        IOrganizationAccessService accessService,
        CancellationToken cancellationToken)
    {
        var access = await GetManagerAccessAsync(
            organizationId,
            principal,
            accessService,
            cancellationToken);
        if (access.Result is not null)
        {
            return access.Result;
        }

        var connection = await dbContext.GameServerConnections.SingleOrDefaultAsync(
            item => item.OrganizationId == organizationId,
            cancellationToken);
        if (connection is null)
        {
            return Results.NoContent();
        }

        dbContext.GameServerConnections.Remove(connection);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static IResult? Validate(
        string? displayName,
        string? host,
        int port,
        string? apiToken,
        string? certificateFingerprint,
        bool tokenRequired)
    {
        if (string.IsNullOrWhiteSpace(displayName)
            || displayName.Trim().Length > 120)
        {
            return Validation(
                "DisplayName",
                "Der Anzeigename muss 1 bis 120 Zeichen enthalten.");
        }

        if (string.IsNullOrWhiteSpace(host)
            || !ServerAddressPolicy.IsValidHost(host.Trim()))
        {
            return Validation(
                "Host",
                "Bitte nur einen Hostnamen oder eine IP-Adresse ohne https:// und ohne Pfad eintragen.");
        }

        if (port is < 1 or > 65535)
        {
            return Validation("Port", "Der Port muss zwischen 1 und 65535 liegen.");
        }

        if (tokenRequired && string.IsNullOrWhiteSpace(apiToken))
        {
            return Validation("ApiToken", "Ein API-Token ist erforderlich.");
        }

        if (apiToken?.Trim().Length > 8000)
        {
            return Validation(
                "ApiToken",
                "Das API-Token ist ungewöhnlich lang.");
        }

        if (!string.IsNullOrWhiteSpace(certificateFingerprint)
            && NormalizeFingerprint(certificateFingerprint) is null)
        {
            return Validation(
                "CertificateFingerprint",
                "Der SHA-256-Fingerprint muss aus genau 64 Hex-Zeichen bestehen.");
        }

        return null;
    }

    private static string? NormalizeFingerprint(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.Any(character =>
            !Uri.IsHexDigit(character)
            && character is not ':' and not '-' and not ' '))
        {
            return null;
        }

        var normalized = new string(
            value.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        return normalized.Length == 64 ? normalized : null;
    }

    private static GameServerConfigurationResponse ToResponse(
        GameServerConnection connection) =>
        new(
            connection.Id,
            connection.DisplayName,
            connection.Host,
            connection.Port,
            !string.IsNullOrWhiteSpace(connection.ProtectedApiToken),
            connection.CertificateFingerprint,
            connection.IsEnabled,
            connection.UpdatedAt,
            connection.ConcurrencyToken);

    private static LiveServerStatusResponse ToResponse(LiveServerStatus status) =>
        new(
            status.State,
            status.DisplayName,
            status.Host,
            status.Port,
            status.Health,
            status.ActiveSessionName,
            status.ConnectedPlayers,
            status.PlayerLimit,
            status.TechTier,
            status.ActiveSchematic,
            status.GamePhase,
            status.IsGameRunning,
            status.IsGamePaused,
            status.TotalGameDurationSeconds,
            status.AverageTickRate,
            status.CheckedAt,
            status.Message,
            status.PresentedCertificateFingerprint);

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(
            new Dictionary<string, string[]> { [key] = [message] });

    private static async Task<AccessResult> GetManagerAccessAsync(
        Guid organizationId,
        ClaimsPrincipal principal,
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
            return access;
        }

        return access.Membership!.PermissionRole.CanManageOrganization()
            ? access
            : new AccessResult(access.Membership, Results.Forbid());
    }

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
