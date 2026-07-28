using CommunityIntranet.Modules.ThemePacks.Configuration;
using CommunityIntranet.Modules.ThemePacks.Domain;
using CommunityIntranet.Modules.ThemePacks.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.ThemePacks.Seeding;

public sealed class ThemePackSeeder(
    IThemePackDbContext dbContext,
    ThemePackSerializer serializer,
    TimeProvider timeProvider)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var configuration in ThemePackSeeds.All)
        {
            var exists = await dbContext.ThemePacks.AnyAsync(
                themePack =>
                    themePack.Key == configuration.Key
                    && themePack.Version == configuration.Version,
                cancellationToken);
            if (exists)
            {
                continue;
            }

            dbContext.ThemePacks.Add(new ThemePack
            {
                Id = Guid.NewGuid(),
                Key = configuration.Key,
                Name = configuration.Name,
                Description = configuration.Description,
                Version = configuration.Version,
                Author = configuration.Author,
                IsSystemTheme = true,
                ConfigurationJson = serializer.Serialize(configuration),
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
