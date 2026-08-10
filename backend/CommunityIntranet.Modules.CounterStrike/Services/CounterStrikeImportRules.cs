using CommunityIntranet.Modules.CounterStrike.Domain;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public static class CounterStrikeImportProjection
{
    public static void Apply(
        CounterStrikeMatch match,
        AnalyzerMatchDto source,
        IReadOnlyDictionary<string, Guid> linkedUsers)
    {
        var linkedTeam = source.Players.Values
            .Where(player => linkedUsers.ContainsKey(SteamId(player.SteamId)))
            .GroupBy(player => player.Team.Name)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();

        match.MapName = NormalizeMapName(source.MapName);
        match.PlayedAt = source.Date == default ? match.UploadedAt : source.Date;
        match.TeamAName = Clean(source.TeamA.Name, 120, "Team A");
        match.TeamBName = Clean(source.TeamB.Name, 120, "Team B");
        match.TeamAScore = source.TeamA.Score;
        match.TeamBScore = source.TeamB.Score;
        match.CommunityTeam = string.Equals(linkedTeam, source.TeamA.Name, StringComparison.Ordinal)
            ? "A"
            : string.Equals(linkedTeam, source.TeamB.Name, StringComparison.Ordinal) ? "B" : null;
    }

    private static string NormalizeMapName(string value)
    {
        var normalized = value.Replace("de_", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        if (normalized.Length == 0)
        {
            return "Unknown";
        }
        return normalized[..Math.Min(normalized.Length, 80)];
    }

    private static string Clean(string? value, int maximumLength, string fallback)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return clean[..Math.Min(clean.Length, maximumLength)];
    }

    private static string SteamId(ulong value) =>
        value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public sealed record CounterStrikeSeasonAggregate(
    Guid UserId,
    int Matches,
    int Wins,
    int Kills,
    int Deaths,
    int Assists,
    double Adr,
    double Kast,
    double HeadshotPercent,
    double HltvRating,
    int UtilityDamage,
    int FirstKills,
    int FirstDeaths,
    int TradeKills,
    int ThreeKills,
    int FourKills,
    int Aces,
    int ClutchesWon);

public static class CounterStrikeSeasonAggregation
{
    public static IReadOnlyDictionary<Guid, CounterStrikeSeasonAggregate> Build(
        IEnumerable<CounterStrikeMatchPlayer> players,
        IReadOnlyDictionary<Guid, CounterStrikeMatch> matches) =>
        players
            .Where(player => player.UserId is not null && matches.ContainsKey(player.MatchId))
            .GroupBy(player => player.UserId!.Value)
            .ToDictionary(
                group => group.Key,
                group => Aggregate(group.Key, group.ToArray(), matches));

    private static CounterStrikeSeasonAggregate Aggregate(
        Guid userId,
        IReadOnlyCollection<CounterStrikeMatchPlayer> rows,
        IReadOnlyDictionary<Guid, CounterStrikeMatch> matches) =>
        new(
            userId,
            rows.Count,
            rows.Count(player => IsWin(matches[player.MatchId], player.TeamName)),
            rows.Sum(player => player.Kills),
            rows.Sum(player => player.Deaths),
            rows.Sum(player => player.Assists),
            rows.Average(player => player.Adr),
            rows.Average(player => player.Kast),
            rows.Average(player => player.HeadshotPercent),
            rows.Average(player => player.HltvRating),
            rows.Sum(player => player.UtilityDamage),
            rows.Sum(player => player.FirstKills),
            rows.Sum(player => player.FirstDeaths),
            rows.Sum(player => player.TradeKills),
            rows.Sum(player => player.ThreeKills),
            rows.Sum(player => player.FourKills),
            rows.Sum(player => player.Aces),
            rows.Sum(player => player.ClutchesWon));

    private static bool IsWin(CounterStrikeMatch match, string teamName) =>
        string.Equals(teamName, match.TeamAName, StringComparison.Ordinal)
            ? match.TeamAScore > match.TeamBScore
            : string.Equals(teamName, match.TeamBName, StringComparison.Ordinal)
                && match.TeamBScore > match.TeamAScore;
}
