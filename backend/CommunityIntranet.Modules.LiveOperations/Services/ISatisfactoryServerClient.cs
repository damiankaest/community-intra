using CommunityIntranet.BuildingBlocks.LiveOperations;

namespace CommunityIntranet.Modules.LiveOperations.Services;

public interface ISatisfactoryServerClient
{
    Task<LiveServerStatus> ProbeAsync(
        SatisfactoryServerTarget target,
        CancellationToken cancellationToken);
}

public sealed record SatisfactoryServerTarget(
    string DisplayName,
    string Host,
    int Port,
    string ApiToken,
    string? CertificateFingerprint);
