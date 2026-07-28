using CommunityIntranet.Modules.ThemePacks.Configuration;

namespace CommunityIntranet.Modules.ThemePacks.Contracts;

public sealed record ThemePackResponse(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Author,
    bool IsSystemTheme,
    ThemePackConfiguration Configuration);

public sealed record ThemePackDefinition(
    Guid Id,
    string Key,
    string Name,
    string Description,
    string Version,
    string Author,
    bool IsSystemTheme,
    ThemePackConfiguration Configuration);
