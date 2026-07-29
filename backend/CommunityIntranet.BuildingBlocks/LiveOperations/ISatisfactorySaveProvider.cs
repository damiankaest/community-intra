namespace CommunityIntranet.BuildingBlocks.LiveOperations;

public interface ISatisfactorySaveProvider
{
    Task<ServerSaveFetchResult> DownloadAsync(
        Guid organizationId,
        string? saveName,
        CancellationToken cancellationToken);
}

public sealed record ServerSaveFetchResult(
    ServerSaveFetchState State,
    string? FileName,
    byte[]? Content,
    string Message);

public enum ServerSaveFetchState
{
    Downloaded,
    NotConfigured,
    Disabled,
    AuthenticationFailed,
    CertificateError,
    NotFound,
    Unavailable,
    ConfigurationError
}
