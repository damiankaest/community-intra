using CommunityIntranet.Modules.Football.Domain;
using Microsoft.EntityFrameworkCore;

namespace CommunityIntranet.Modules.Football.Persistence;

public sealed class FootballDbContext(DbContextOptions<FootballDbContext> options)
    : DbContext(options), IFootballDbContext
{
    public DbSet<FootballMemberProfile> FootballMemberProfiles => Set<FootballMemberProfile>();
    public DbSet<FootballExercise> FootballExercises => Set<FootballExercise>();
    public DbSet<FootballSession> FootballSessions => Set<FootballSession>();
    public DbSet<FootballAttendance> FootballAttendances => Set<FootballAttendance>();
    public DbSet<FootballTrainingBlock> FootballTrainingBlocks => Set<FootballTrainingBlock>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FootballDbContext).Assembly);
    }
}
