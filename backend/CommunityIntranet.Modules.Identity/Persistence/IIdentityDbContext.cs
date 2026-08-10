using CommunityIntranet.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Identity.Persistence;

public interface IIdentityDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<SteamIdentity> SteamIdentities { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
