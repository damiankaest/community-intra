using CommunityIntranet.Modules.Football.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Football.Persistence;

public interface IFootballDbContext
{
    DbSet<FootballMemberProfile> FootballMemberProfiles { get; }
    DbSet<FootballExercise> FootballExercises { get; }
    DbSet<FootballSession> FootballSessions { get; }
    DbSet<FootballAttendance> FootballAttendances { get; }
    DbSet<FootballTrainingBlock> FootballTrainingBlocks { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
