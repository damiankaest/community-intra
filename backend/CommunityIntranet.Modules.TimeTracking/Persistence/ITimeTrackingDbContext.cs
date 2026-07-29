using CommunityIntranet.Modules.TimeTracking.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.TimeTracking.Persistence;

public interface ITimeTrackingDbContext
{
    DbSet<WorkShift> WorkShifts { get; }

    DbSet<WorkLogEntry> WorkLogEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
