using System.Security.Cryptography;
using CommunityIntranet.BuildingBlocks.LiveOperations;
using CommunityIntranet.Modules.LiveOperations.Domain;
using CommunityIntranet.Modules.LiveOperations.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CommunityIntranet.Modules.LiveOperations.Services;

public sealed class LiveOperationsReader(
    ILiveOperationsDbContext dbContext,
    IGameServerTokenProtector tokenProtector,
    ISatisfactoryServerClient serverClient,
    IMemoryCache memoryCache,
    TimeProvider timeProvider)
    : ILiveOperationsReader
{
    private static readonly TimeSpan OnlineCacheDuration =
        TimeSpan.FromSeconds(20);
    private static readonly TimeSpan FailureCacheDuration =
        TimeSpan.FromSeconds(5);

    public async Task<LiveServerStatus> GetServerStatusAsync(
        Guid organizationId,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var connection = await dbContext.GameServerConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.OrganizationId == organizationId,
                cancellationToken);
        if (connection is null)
        {
            return Empty(
                LiveServerConnectionState.NotConfigured,
                "Noch kein Gameserver verbunden.");
        }

        if (!connection.IsEnabled)
        {
            return new LiveServerStatus(
                LiveServerConnectionState.Disabled,
                connection.DisplayName,
                connection.Host,
                connection.Port,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                timeProvider.GetUtcNow(),
                "Die Serveranbindung ist derzeit ausgeschaltet.");
        }

        var cacheKey =
            $"live-server:{organizationId:N}:{connection.ConcurrencyToken:N}";
        if (!forceRefresh
            && memoryCache.TryGetValue(
                cacheKey,
                out LiveServerStatus? cachedStatus)
            && cachedStatus is not null)
        {
            return cachedStatus;
        }

        string apiToken;
        try
        {
            apiToken = tokenProtector.Unprotect(
                organizationId,
                connection.ProtectedApiToken);
        }
        catch (CryptographicException)
        {
            return InvalidToken(connection);
        }
        catch (InvalidOperationException)
        {
            return InvalidToken(connection);
        }

        var status = await serverClient.ProbeAsync(
            new SatisfactoryServerTarget(
                connection.DisplayName,
                connection.Host,
                connection.Port,
                apiToken,
                connection.CertificateFingerprint),
            cancellationToken);
        memoryCache.Set(
            cacheKey,
            status,
            status.State == LiveServerConnectionState.Online
                ? OnlineCacheDuration
                : FailureCacheDuration);
        return status;
    }

    private LiveServerStatus InvalidToken(
        GameServerConnection connection) =>
        new(
            LiveServerConnectionState.ConfigurationError,
            connection.DisplayName,
            connection.Host,
            connection.Port,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            timeProvider.GetUtcNow(),
            "Das gespeicherte API-Token kann nicht mehr entschlüsselt werden. Bitte neu eintragen.");

    private LiveServerStatus Empty(
        LiveServerConnectionState state,
        string message) =>
        new(
            state,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            timeProvider.GetUtcNow(),
            message);
}
