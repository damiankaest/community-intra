using CommunityIntranet.Modules.ThemePacks.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.ThemePacks.Persistence;

public interface IThemePackDbContext
{
    DbSet<ThemePack> ThemePacks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
