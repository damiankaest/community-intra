using CommunityIntranet.BuildingBlocks.LiveOperations;

namespace CommunityIntranet.Modules.LiveOperations.Services;

public interface ISatisfactoryServerClient
{
    Task<LiveServerStatus> ProbeAsync(
        SatisfactoryServerTarget target,
        CancellationToken cancellationToken);

    Task<ServerSaveDownloadResult> DownloadSaveAsync(
        SatisfactoryServerTarget target,
        string? saveName,
        CancellationToken cancellationToken);
}

public sealed record SatisfactoryServerTarget(
    string DisplayName,
    string Host,
    int Port,
    string ApiToken,
    string? CertificateFingerprint);

public sealed record ServerSaveDownloadResult(
    ServerSaveDownloadState State,
    string? FileName,
    byte[]? Content,
    string Message);

public enum ServerSaveDownloadState
{
    Downloaded,
    AuthenticationFailed,
    CertificateError,
    NotFound,
    Unavailable,
    ConfigurationError
}
