using CommunityIntranet.Modules.CounterStrike.Services;
using Xunit;

namespace CommunityIntranet.Api.Tests.CounterStrike;

public sealed class HighlightRuleTests
{
    [Theory]
    [InlineData(3, "3K", 68)]
    [InlineData(4, "4K", 86)]
    [InlineData(5, "Ace", 100)]
    public void MultiKillRuleDetectsExpectedHighlight(int killCount, string type, int score)
    {
        var match = new AnalyzerMatchDto
        {
            Kills = Enumerable.Range(1, killCount).Select(index => new AnalyzerKillDto
            {
                Tick = index * 100,
                RoundNumber = 7,
                KillerSteamId = 76561198000000001,
                KillerName = "Damian",
                KillerTeamName = "CouchClash",
                VictimSteamId = (ulong)(76561198000000010 + index),
                VictimTeamName = "Opponents"
            }).ToList()
        };

        var highlight = Assert.Single(new MultiKillHighlightRule().Evaluate(new CounterStrikeHighlightContext(match)));

        Assert.Equal(type, highlight.Type);
        Assert.Equal(score, highlight.Score);
        Assert.Equal(7, highlight.RoundNumber);
    }

    [Fact]
    public void MultiKillRuleIgnoresTeamKillsAndSuicides()
    {
        var match = new AnalyzerMatchDto
        {
            Kills =
            [
                Kill(1, "CouchClash", "CouchClash", 11, 12),
                Kill(1, "CouchClash", "Opponents", 11, 11),
                Kill(1, "CouchClash", "Opponents", 11, 13)
            ]
        };

        Assert.Empty(new MultiKillHighlightRule().Evaluate(new CounterStrikeHighlightContext(match)));
    }

    [Theory]
    [InlineData(2, "1v2", 80)]
    [InlineData(3, "1v3", 87)]
    [InlineData(5, "1v5", 100)]
    public void ClutchRuleDetectsWonOneVsX(int opponents, string type, int score)
    {
        var match = new AnalyzerMatchDto
        {
            Clutches =
            [
                new AnalyzerClutchDto
                {
                    Tick = 900,
                    RoundNumber = 12,
                    OpponentCount = opponents,
                    HasWon = true,
                    ClutcherSteamId = 11,
                    ClutcherName = "Damian"
                }
            ]
        };

        var highlight = Assert.Single(new ClutchHighlightRule().Evaluate(new CounterStrikeHighlightContext(match)));

        Assert.Equal(type, highlight.Type);
        Assert.Equal(score, highlight.Score);
    }

    [Fact]
    public void SpecialKillRuleDetectsNoScopeBeforeOtherFlags()
    {
        var match = new AnalyzerMatchDto
        {
            Kills =
            [
                new AnalyzerKillDto
                {
                    Tick = 100,
                    RoundNumber = 2,
                    KillerSteamId = 11,
                    KillerName = "AWPer",
                    IsNoScope = true,
                    IsThroughSmoke = true,
                    PenetratedObjects = 1
                }
            ]
        };

        var highlight = Assert.Single(new SpecialKillHighlightRule().Evaluate(new CounterStrikeHighlightContext(match)));

        Assert.Equal("No-Scope", highlight.Type);
    }

    private static AnalyzerKillDto Kill(int round, string killerTeam, string victimTeam, ulong killer, ulong victim) => new()
    {
        RoundNumber = round,
        KillerTeamName = killerTeam,
        VictimTeamName = victimTeam,
        KillerSteamId = killer,
        VictimSteamId = victim,
        KillerName = "Player"
    };
}
