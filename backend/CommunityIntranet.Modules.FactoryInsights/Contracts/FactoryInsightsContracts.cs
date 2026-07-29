using CommunityIntranet.BuildingBlocks.LiveOperations;
using CommunityIntranet.Modules.FactoryInsights.Domain;

namespace CommunityIntranet.Modules.FactoryInsights.Contracts;

public sealed record SaveFactoryRequest(
    string? Name,
    string? Description,
    double? CenterX,
    double? CenterY,
    double? RadiusMeters);

public sealed record FactorySiteResponse(
    Guid Id,
    string Name,
    string? Description,
    double? CenterX,
    double? CenterY,
    double? RadiusMeters,
    int? MachineCount,
    int? BuildableCount,
    DateTimeOffset UpdatedAt,
    Guid ConcurrencyToken);

public sealed record SaveSnapshotResponse(
    Guid Id,
    SaveImportSource Source,
    string OriginalFileName,
    string ContentSha256,
    long FileSizeBytes,
    string? SaveName,
    string? SessionName,
    string? MapName,
    int? SaveVersion,
    int? BuildVersion,
    long? PlayDurationSeconds,
    DateTimeOffset? SavedAt,
    bool? IsModdedSave,
    string ParserVersion,
    DateTimeOffset ImportedAt,
    SaveAnalysis? Analysis);

public sealed record FactoryInsightsOverviewResponse(
    IReadOnlyList<FactorySiteResponse> Factories,
    SaveSnapshotResponse? LatestSnapshot,
    IReadOnlyList<SaveSnapshotResponse> RecentSnapshots,
    bool SaveParserAvailable,
    LiveServerConnectionState ServerState,
    string ServerMessage);

public sealed record ServerSaveImportRequest(string? SaveName);

public sealed record SaveAnalysis(
    string ParserVersion,
    string? SaveName,
    string? SessionName,
    string? MapName,
    int? SaveVersion,
    int? BuildVersion,
    long? PlayDurationSeconds,
    DateTimeOffset? SavedAt,
    bool? IsModdedSave,
    SaveTotals Totals,
    WorldBounds? Bounds,
    IReadOnlyList<BuildingTypeSummary> BuildingTypes,
    IReadOnlyList<DetectedFactoryArea> DetectedAreas);

public sealed record SaveTotals(
    int Objects,
    int Buildables,
    int ProductionMachines,
    int Extractors,
    int PowerBuildings,
    int Logistics,
    int StorageBuildings,
    int TransportBuildings,
    int Foundations,
    int OtherBuildables);

public sealed record WorldBounds(
    double MinimumX,
    double MinimumY,
    double MaximumX,
    double MaximumY);

public sealed record BuildingTypeSummary(
    string TypePath,
    string ClassName,
    string DisplayName,
    string Category,
    int Count);

public sealed record DetectedFactoryArea(
    string Key,
    string SuggestedName,
    double CenterX,
    double CenterY,
    double RadiusMeters,
    int MachineCount,
    int BuildableCount,
    IReadOnlyList<BuildingTypeCount> TopBuildingTypes);

public sealed record BuildingTypeCount(string DisplayName, int Count);
