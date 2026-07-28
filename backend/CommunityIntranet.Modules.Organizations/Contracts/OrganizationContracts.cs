using CommunityIntranet.BuildingBlocks.Authorization;

namespace CommunityIntranet.Modules.Organizations.Contracts;

public sealed record CreateOrganizationRequest(
    string Name,
    string? Description,
    string Language,
    string TimeZone,
    string? VisibleTitle,
    string? ThemePackKey = null,
    IReadOnlyList<string>? EnabledModules = null);

public sealed record UpdateOrganizationRequest(
    string Name,
    string? Description,
    string Language,
    string TimeZone,
    string? ThemePackKey = null,
    IReadOnlyList<string>? EnabledModules = null);

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ThemePackId,
    string ThemePackKey,
    string ThemePackVersion,
    IReadOnlyList<string> EnabledModules,
    string Language,
    string TimeZone,
    Guid OwnerUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsArchived,
    PermissionRole PermissionRole,
    string? VisibleTitle);

public sealed record OrganizationSummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string ThemePackKey,
    string ThemePackVersion,
    string Language,
    PermissionRole PermissionRole,
    string? VisibleTitle);

public static class OrganizationModuleKeys
{
    public const string Projects = "projects";
    public const string Tasks = "tasks";
    public const string Incidents = "incidents";
    public const string Awards = "awards";
    public const string ActivityFeed = "activity-feed";

    public static IReadOnlySet<string> All { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            Projects,
            Tasks,
            Incidents,
            Awards,
            ActivityFeed
        };

    public static IReadOnlyList<string> Defaults { get; } =
        [Projects, Tasks, Incidents, Awards, ActivityFeed];

    public static List<string> Normalize(IReadOnlyList<string>? values) =>
        values is null || values.Count == 0
            ? [.. Defaults]
            : values
                .Select(value => value.Trim().ToLowerInvariant())
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();
}
