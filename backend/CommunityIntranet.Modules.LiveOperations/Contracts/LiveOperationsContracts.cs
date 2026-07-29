using CommunityIntranet.BuildingBlocks.LiveOperations;

namespace CommunityIntranet.Modules.LiveOperations.Contracts;

public sealed record GameServerConfigurationResponse(
    Guid Id,
    string DisplayName,
    string Host,
    int Port,
    bool HasApiToken,
    string? CertificateFingerprint,
    bool IsEnabled,
    DateTimeOffset UpdatedAt,
    Guid ConcurrencyToken);

public sealed record SaveGameServerConfigurationRequest(
    string? DisplayName,
    string? Host,
    int Port,
    string? ApiToken,
    string? CertificateFingerprint,
    bool IsEnabled = true,
    Guid? ConcurrencyToken = null);

public sealed record TestGameServerConnectionRequest(
    string? DisplayName,
    string? Host,
    int Port,
    string? ApiToken,
    string? CertificateFingerprint);

public sealed record LiveServerStatusResponse(
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
    string? PresentedCertificateFingerprint);
