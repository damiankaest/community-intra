using CommunityIntranet.Modules.Incidents.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Incidents.Persistence;

public interface IIncidentDbContext
{
    DbSet<Incident> Incidents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
