using System.Globalization;
using CommunityIntranet.Modules.CounterStrike.Domain;
using CommunityIntranet.Modules.CounterStrike.Persistence;
using CommunityIntranet.Modules.CounterStrike.Services;
using CommunityIntranet.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommunityIntranet.Api.Tests.CounterStrike;

public sealed class TrainingAwardAndMappingTests
{
    [Fact]
    public void UtilityRuleRecommendsTrainingBelowThreshold()
    {
        var stats = Stats(matches: 5);
        stats.UtilityDamage = 40;

        var recommendation = new UtilityTrainingRecommendationRule().Evaluate(stats);

        Assert.NotNull(recommendation);
        Assert.Equal(CounterStrikeTrainingKind.Utility, recommendation.Kind);
    }

    [Fact]
    public void EntryRuleUsesFirstDuelDifference()
    {
        var stats = Stats(matches: 4);
        stats.FirstKills = 3;
        stats.FirstDeaths = 8;

        var recommendation = new FirstDuelTrainingRecommendationRule().Evaluate(stats);

        Assert.NotNull(recommendation);
        Assert.Equal("first-duels", recommendation.Key);
    }

    [Fact]
    public void MvpRuleChoosesHighestRating()
    {
        var first = Stats(matches: 5);
        first.UserId = Guid.NewGuid();
        first.HltvRating = 1.08;
        var second = Stats(matches: 5);
        second.UserId = Guid.NewGuid();
        second.HltvRating = 1.31;

        var award = new MvpAwardRule().Evaluate([first, second]);

        Assert.NotNull(award);
        Assert.Equal(second.UserId, award.UserId);
    }

    [Fact]
    public void SteamMapperMapsOnlyPlayersPresentInDemo()
    {
        var mappedUserId = Guid.NewGuid();
        var identities = new[]
        {
            new SteamIdentity { Id = Guid.NewGuid(), UserId = mappedUserId, SteamId64 = "76561198000000001", DisplayName = "Mapped" },
            new SteamIdentity { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), SteamId64 = "76561198000000002", DisplayName = "Absent" }
        };
        var players = new[] { new AnalyzerPlayerDto { SteamId = 76561198000000001, Name = "Mapped" } };

        var result = SteamIdentityMapper.Map(identities, players);

        Assert.Single(result);
        Assert.Equal(mappedUserId, result["76561198000000001"]);
    }

    [Fact]
    public void MatchProjectionMapsScoreMapAndCommunityTeam()
    {
        const ulong steamId = 76561198000000001;
        var match = Match(Guid.NewGuid(), 0, 0);
        var source = new AnalyzerMatchDto
        {
            MapName = "de_ancient",
            Date = new DateTimeOffset(2026, 8, 8, 20, 0, 0, TimeSpan.Zero),
            TeamA = new AnalyzerTeamDto { Name = "CouchClash", Score = 13 },
            TeamB = new AnalyzerTeamDto { Name = "Opponent", Score = 7 },
            Players = new Dictionary<string, AnalyzerPlayerDto>
            {
                [steamId.ToString(CultureInfo.InvariantCulture)] = new()
                {
                    SteamId = steamId,
                    Name = "Mapped",
                    Team = new AnalyzerTeamDto { Name = "CouchClash" }
                }
            }
        };

        CounterStrikeImportProjection.Apply(
            match,
            source,
            new Dictionary<string, Guid>
            {
                [steamId.ToString(CultureInfo.InvariantCulture)] = Guid.NewGuid()
            });

        Assert.Equal("ancient", match.MapName);
        Assert.Equal(13, match.TeamAScore);
        Assert.Equal(7, match.TeamBScore);
        Assert.Equal("A", match.CommunityTeam);
        Assert.Equal(source.Date, match.PlayedAt);
    }

    [Fact]
    public void SeasonAggregationCombinesMatchesAndWins()
    {
        var userId = Guid.NewGuid();
        var first = Match(Guid.NewGuid(), 13, 8);
        var second = Match(Guid.NewGuid(), 10, 13);
        var players = new[]
        {
            MatchPlayer(first.Id, userId, kills: 20, deaths: 10, adr: 90),
            MatchPlayer(second.Id, userId, kills: 10, deaths: 15, adr: 70)
        };

        var result = CounterStrikeSeasonAggregation.Build(
            players,
            new Dictionary<Guid, CounterStrikeMatch> { [first.Id] = first, [second.Id] = second })[userId];

        Assert.Equal(2, result.Matches);
        Assert.Equal(1, result.Wins);
        Assert.Equal(30, result.Kills);
        Assert.Equal(25, result.Deaths);
        Assert.Equal(80, result.Adr);
    }

    [Fact]
    public void SquadReadinessKeepsAcceptancesAboveFive()
    {
        var readiness = CounterStrikeSquadStatistics.BuildReadiness(accepted: 7);

        Assert.True(readiness.FullStack);
        Assert.Equal(0, readiness.Missing);
        Assert.Equal(2, readiness.Substitutes);
    }

    [Fact]
    public void PlayerRecordCombinesEveryMembersPersonalResults()
    {
        var first = Stats(matches: 3);
        first.Wins = 2;
        var second = Stats(matches: 2);

        var record = CounterStrikeSquadStatistics.BuildPlayerRecord([first, second]);

        Assert.Equal(5, record.Matches);
        Assert.Equal(2, record.Wins);
        Assert.Equal(3, record.Losses);
        Assert.Equal(40, record.WinRate);
    }

    [Fact]
    public void TeamRecordOnlyCountsMatchesWithFiveOrganizationMembers()
    {
        var memberIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        var fullStackWin = Match(Guid.NewGuid(), 13, 8);
        var fullStackLoss = Match(Guid.NewGuid(), 9, 13);
        var partialStackWin = Match(Guid.NewGuid(), 13, 4);
        var matches = new[] { fullStackWin, fullStackLoss, partialStackWin };
        var players = memberIds
            .Select(userId => MatchPlayer(fullStackWin.Id, userId))
            .Concat(memberIds.Select(userId => MatchPlayer(fullStackLoss.Id, userId)))
            .Concat(memberIds.Take(4).Select(userId => MatchPlayer(partialStackWin.Id, userId)))
            .ToArray();

        var record = CounterStrikeSquadStatistics.BuildFullSquadRecord(
            matches,
            players,
            memberIds.ToHashSet());

        Assert.Equal(2, record.Matches);
        Assert.Equal(1, record.Wins);
        Assert.Equal(1, record.Losses);
        Assert.Equal(50, record.WinRate);
    }

    [Fact]
    public void DemoChecksumHasUniqueIndexPerOrganization()
    {
        using var dbContext = new CounterStrikeModelTestContext(
            new DbContextOptionsBuilder<CounterStrikeModelTestContext>()
                .UseNpgsql("Host=localhost;Database=model_only")
                .Options);

        var index = Assert.Single(
            dbContext.Model.FindEntityType(typeof(CounterStrikeMatch))!.GetIndexes(),
            item => item.Properties.Select(property => property.Name)
                .SequenceEqual(new[]
                {
                    nameof(CounterStrikeMatch.OrganizationId),
                    nameof(CounterStrikeMatch.DemoChecksum)
                }));

        Assert.True(index.IsUnique);
    }

    private static CounterStrikePlayerStats Stats(int matches) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        SeasonId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Matches = matches
    };

    private static CounterStrikeMatch Match(Guid id, int teamAScore, int teamBScore) => new()
    {
        Id = id,
        OrganizationId = Guid.NewGuid(),
        SeasonId = Guid.NewGuid(),
        UploadedByUserId = Guid.NewGuid(),
        UploadedByMemberId = Guid.NewGuid(),
        DemoChecksum = Guid.NewGuid().ToString("N"),
        OriginalFileName = "match.dem",
        DemoStoragePath = "/data/match.dem",
        TeamAName = "CouchClash",
        TeamBName = "Opponent",
        TeamAScore = teamAScore,
        TeamBScore = teamBScore,
        CommunityTeam = "A",
        Status = CounterStrikeDemoStatus.Completed,
        UploadedAt = DateTimeOffset.UtcNow
    };

    private static CounterStrikeMatchPlayer MatchPlayer(Guid matchId, Guid userId) =>
        MatchPlayer(matchId, userId, kills: 0, deaths: 0, adr: 0);

    private static CounterStrikeMatchPlayer MatchPlayer(
        Guid matchId,
        Guid userId,
        int kills,
        int deaths,
        double adr) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        MatchId = matchId,
        UserId = userId,
        SteamId64 = "76561198000000001",
        DisplayName = "Player",
        TeamName = "CouchClash",
        Kills = kills,
        Deaths = deaths,
        Adr = adr,
        Kast = 70,
        HeadshotPercent = 40,
        HltvRating = 1.1
    };

    private sealed class CounterStrikeModelTestContext(
        DbContextOptions<CounterStrikeModelTestContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            modelBuilder.ApplyConfiguration(new CounterStrikeMatchConfiguration());
    }
}
