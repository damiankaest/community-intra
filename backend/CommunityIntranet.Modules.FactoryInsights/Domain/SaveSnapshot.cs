using CommunityIntranet.BuildingBlocks.Tenancy;

namespace CommunityIntranet.Modules.FactoryInsights.Domain;

public sealed class SaveSnapshot : IOrganizationScoped
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid ImportedByMemberId { get; set; }

    public SaveImportSource Source { get; set; }

    public required string OriginalFileName { get; set; }

    public required string ContentSha256 { get; set; }

    public long FileSizeBytes { get; set; }

    public string? SaveName { get; set; }

    public string? SessionName { get; set; }

    public string? MapName { get; set; }

    public int? SaveVersion { get; set; }

    public int? BuildVersion { get; set; }

    public long? PlayDurationSeconds { get; set; }

    public DateTimeOffset? SavedAt { get; set; }

    public bool? IsModdedSave { get; set; }

    public required string ParserVersion { get; set; }

    public required string AnalysisJson { get; set; }

    public DateTimeOffset ImportedAt { get; set; }
}

public enum SaveImportSource
{
    ManualUpload,
    ServerApi
}
