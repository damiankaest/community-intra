using CommunityIntranet.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Identity.Persistence;

public interface IIdentityDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
