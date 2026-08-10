using CommunityIntranet.Modules.CounterStrike.Domain;
using CommunityIntranet.Modules.Identity.Domain;
using CommunityIntranet.Modules.Members.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CommunityIntranet.Modules.CounterStrike.Persistence;

public interface ICounterStrikeDbContext
{
    DatabaseFacade Database { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<SteamIdentity> SteamIdentities { get; }
    DbSet<OrganizationMember> OrganizationMembers { get; }
    DbSet<CounterStrikeCommunitySettings> CounterStrikeCommunitySettings { get; }
    DbSet<CounterStrikeSeason> CounterStrikeSeasons { get; }
    DbSet<CounterStrikeMatch> CounterStrikeMatches { get; }
    DbSet<CounterStrikeMatchPlayer> CounterStrikeMatchPlayers { get; }
    DbSet<CounterStrikeRound> CounterStrikeRounds { get; }
    DbSet<CounterStrikePlayerStats> CounterStrikePlayerStats { get; }
    DbSet<CounterStrikeHighlight> CounterStrikeHighlights { get; }
    DbSet<CounterStrikeHighlightReaction> CounterStrikeHighlightReactions { get; }
    DbSet<CounterStrikeAward> CounterStrikeAwards { get; }
    DbSet<CounterStrikeAwardAssignment> CounterStrikeAwardAssignments { get; }
    DbSet<CounterStrikeGameSession> CounterStrikeGameSessions { get; }
    DbSet<CounterStrikeGameSessionParticipant> CounterStrikeGameSessionParticipants { get; }
    DbSet<CounterStrikeTrainingPlan> CounterStrikeTrainingPlans { get; }
    DbSet<CounterStrikeTrainingExercise> CounterStrikeTrainingExercises { get; }
    DbSet<CounterStrikeTrainingSession> CounterStrikeTrainingSessions { get; }
    DbSet<CounterStrikeTrainingResult> CounterStrikeTrainingResults { get; }
    DbSet<CounterStrikeWeeklyChallenge> CounterStrikeWeeklyChallenges { get; }
    DbSet<CounterStrikeWeeklyChallengeProgress> CounterStrikeWeeklyChallengeProgress { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
