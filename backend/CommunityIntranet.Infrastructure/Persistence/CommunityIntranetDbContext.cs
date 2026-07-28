using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Infrastructure.Persistence;

public sealed class CommunityIntranetDbContext(
    DbContextOptions<CommunityIntranetDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommunityIntranetDbContext).Assembly);
    }
}
