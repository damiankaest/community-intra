using CommunityIntranet.Modules.Projects.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Projects.Persistence;

public interface IProjectDbContext
{
    DbSet<Project> Projects { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
