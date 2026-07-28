using CommunityIntranet.Modules.ThemePacks.Configuration;
using CommunityIntranet.Modules.ThemePacks.Contracts;
using CommunityIntranet.Modules.ThemePacks.Domain;
using CommunityIntranet.Modules.ThemePacks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.ThemePacks.Services;

public sealed class ThemePackCatalog(
    IThemePackDbContext dbContext,
    ThemePackSerializer serializer)
    : IThemePackCatalog
{
    public async Task<IReadOnlyList<ThemePackDefinition>> ListAsync(
        CancellationToken cancellationToken)
    {
        var themePacks = await dbContext.ThemePacks
            .AsNoTracking()
            .OrderBy(themePack => themePack.Name)
            .ThenBy(themePack => themePack.Version)
            .ToListAsync(cancellationToken);

        return themePacks.Select(ToDefinition).ToArray();
    }

    public async Task<ThemePackDefinition?> FindByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var themePack = await dbContext.ThemePacks
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id,
                cancellationToken);

        return themePack is null ? null : ToDefinition(themePack);
    }

    public async Task<ThemePackDefinition?> FindByKeyAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var normalizedKey = key.Trim().ToLowerInvariant();
        var themePack = await dbContext.ThemePacks
            .AsNoTracking()
            .Where(item => item.Key == normalizedKey)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return themePack is null ? null : ToDefinition(themePack);
    }

    private ThemePackDefinition ToDefinition(ThemePack themePack) =>
        new(
            themePack.Id,
            themePack.Key,
            themePack.Name,
            themePack.Description,
            themePack.Version,
            themePack.Author,
            themePack.IsSystemTheme,
            serializer.Deserialize(themePack.ConfigurationJson));
}
