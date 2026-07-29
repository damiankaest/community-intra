namespace CommunityIntranet.BuildingBlocks.LiveOperations;

public interface ILiveOperationsReader
{
    Task<LiveServerStatus> GetServerStatusAsync(
        Guid organizationId,
        bool forceRefresh,
        CancellationToken cancellationToken);
}

public enum LiveServerConnectionState
{
    NotConfigured,
    Disabled,
    Online,
    Reachable,
    Offline,
    AuthenticationFailed,
    UntrustedCertificate,
    CertificateChanged,
    ConfigurationError
}

public sealed record LiveServerStatus(
    LiveServerConnectionState State,
    string? DisplayName,
    string? Host,
    int? Port,
    string? Health,
    string? ActiveSessionName,
    int? ConnectedPlayers,
    int? PlayerLimit,
    int? TechTier,
    string? ActiveSchematic,
    string? GamePhase,
    bool? IsGameRunning,
    bool? IsGamePaused,
    long? TotalGameDurationSeconds,
    double? AverageTickRate,
    DateTimeOffset CheckedAt,
    string Message,
    string? PresentedCertificateFingerprint = null);
