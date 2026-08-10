using CommunityIntranet.Modules.CounterStrike.Domain;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed record CounterStrikeSquadReadiness(
    int Missing,
    int Substitutes,
    bool FullStack);

public sealed record CounterStrikeRecord(
    int Matches,
    int Wins,
    int Losses,
    double WinRate);

public static class CounterStrikeSquadStatistics
{
    private const int FullSquadSize = 5;

    public static CounterStrikeSquadReadiness BuildReadiness(int accepted)
    {
        var normalized = Math.Max(0, accepted);
        return new CounterStrikeSquadReadiness(
            Math.Max(0, FullSquadSize - normalized),
            Math.Max(0, normalized - FullSquadSize),
            normalized >= FullSquadSize);
    }

    public static CounterStrikeRecord BuildPlayerRecord(
        IEnumerable<CounterStrikePlayerStats> playerStats)
    {
        var rows = playerStats.ToArray();
        var matches = rows.Sum(item => item.Matches);
        var wins = rows.Sum(item => item.Wins);
        return BuildRecord(matches, wins);
    }

    public static CounterStrikeRecord BuildFullSquadRecord(
        IEnumerable<CounterStrikeMatch> matches,
        IEnumerable<CounterStrikeMatchPlayer> players,
        IReadOnlySet<Guid> organizationMemberIds)
    {
        var matchById = matches
            .Where(match => match.Status == CounterStrikeDemoStatus.Completed
                && match.CommunityTeam is "A" or "B")
            .ToDictionary(match => match.Id);
        var fullSquadMatchIds = players
            .Where(player => player.UserId is { } userId
                && organizationMemberIds.Contains(userId)
                && matchById.TryGetValue(player.MatchId, out var match)
                && IsCommunityPlayer(match, player))
            .GroupBy(player => player.MatchId)
            .Where(group => group
                .Select(player => player.UserId!.Value)
                .Distinct()
                .Count() == FullSquadSize)
            .Select(group => group.Key)
            .ToHashSet();
        var fullSquadMatches = matchById.Values
            .Where(match => fullSquadMatchIds.Contains(match.Id))
            .ToArray();
        var wins = fullSquadMatches.Count(IsCommunityWin);
        return BuildRecord(fullSquadMatches.Length, wins);
    }

    private static CounterStrikeRecord BuildRecord(int matches, int wins) => new(
        matches,
        wins,
        matches - wins,
        matches == 0 ? 0 : wins * 100d / matches);

    private static bool IsCommunityPlayer(
        CounterStrikeMatch match,
        CounterStrikeMatchPlayer player)
    {
        var communityTeamName = match.CommunityTeam == "A"
            ? match.TeamAName
            : match.TeamBName;
        return string.Equals(player.TeamName, communityTeamName, StringComparison.Ordinal);
    }

    private static bool IsCommunityWin(CounterStrikeMatch match) =>
        match.CommunityTeam == "A"
            ? match.TeamAScore > match.TeamBScore
            : match.TeamBScore > match.TeamAScore;
}
