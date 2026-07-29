using System.Security.Cryptography;
using CommunityIntranet.BuildingBlocks.LiveOperations;
using CommunityIntranet.Modules.LiveOperations.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.LiveOperations.Services;

public sealed class SatisfactorySaveProvider(
    ILiveOperationsDbContext dbContext,
    IGameServerTokenProtector tokenProtector,
    ISatisfactoryServerClient serverClient)
    : ISatisfactorySaveProvider
{
    public async Task<ServerSaveFetchResult> DownloadAsync(
        Guid organizationId,
        string? saveName,
        CancellationToken cancellationToken)
    {
        var connection = await dbContext.GameServerConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.OrganizationId == organizationId,
                cancellationToken);
        if (connection is null)
        {
            return Failure(
                ServerSaveFetchState.NotConfigured,
                "Noch kein Gameserver verbunden.");
        }

        if (!connection.IsEnabled)
        {
            return Failure(
                ServerSaveFetchState.Disabled,
                "Die Serveranbindung ist ausgeschaltet.");
        }

        string token;
        try
        {
            token = tokenProtector.Unprotect(
                organizationId,
                connection.ProtectedApiToken);
        }
        catch (CryptographicException)
        {
            return Failure(
                ServerSaveFetchState.ConfigurationError,
                "Das gespeicherte API-Token kann nicht entschlüsselt werden.");
        }
        catch (InvalidOperationException)
        {
            return Failure(
                ServerSaveFetchState.ConfigurationError,
                "Das gespeicherte API-Token kann nicht entschlüsselt werden.");
        }

        var result = await serverClient.DownloadSaveAsync(
            new SatisfactoryServerTarget(
                connection.DisplayName,
                connection.Host,
                connection.Port,
                token,
                connection.CertificateFingerprint),
            saveName,
            cancellationToken);
        return new ServerSaveFetchResult(
            result.State switch
            {
                ServerSaveDownloadState.Downloaded =>
                    ServerSaveFetchState.Downloaded,
                ServerSaveDownloadState.AuthenticationFailed =>
                    ServerSaveFetchState.AuthenticationFailed,
                ServerSaveDownloadState.CertificateError =>
                    ServerSaveFetchState.CertificateError,
                ServerSaveDownloadState.NotFound =>
                    ServerSaveFetchState.NotFound,
                ServerSaveDownloadState.ConfigurationError =>
                    ServerSaveFetchState.ConfigurationError,
                _ => ServerSaveFetchState.Unavailable
            },
            result.FileName,
            result.Content,
            result.Message);
    }

    private static ServerSaveFetchResult Failure(
        ServerSaveFetchState state,
        string message) =>
        new(state, null, null, message);
}
