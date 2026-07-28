using CommunityIntranet.Modules.ThemePacks.Contracts;

namespace CommunityIntranet.Modules.ThemePacks.Services;

public interface IThemePackCatalog
{
    Task<IReadOnlyList<ThemePackDefinition>> ListAsync(
        CancellationToken cancellationToken);

    Task<ThemePackDefinition?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<ThemePackDefinition?> FindByKeyAsync(
        string key,
        CancellationToken cancellationToken);
}
