namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed record CounterStrikeHighlightCandidate(
    string SteamId64,
    string PlayerName,
    int RoundNumber,
    string Type,
    string Title,
    int Score,
    int StartTick,
    int? EndTick = null);

public sealed record CounterStrikeHighlightContext(AnalyzerMatchDto Match);

public interface IHighlightRule
{
    IReadOnlyCollection<CounterStrikeHighlightCandidate> Evaluate(
        CounterStrikeHighlightContext context);
}

public sealed class MultiKillHighlightRule : IHighlightRule
{
    public IReadOnlyCollection<CounterStrikeHighlightCandidate> Evaluate(
        CounterStrikeHighlightContext context)
    {
        var candidates = new List<CounterStrikeHighlightCandidate>();
        var groups = context.Match.Kills
            .Where(kill => kill.KillerSteamId != 0
                && kill.KillerSteamId != kill.VictimSteamId
                && !string.Equals(kill.KillerTeamName, kill.VictimTeamName, StringComparison.Ordinal))
            .GroupBy(kill => new { kill.RoundNumber, kill.KillerSteamId, kill.KillerName });
        foreach (var group in groups)
        {
            var kills = group.OrderBy(kill => kill.Tick).ToArray();
            if (kills.Length < 3)
            {
                continue;
            }

            var type = kills.Length >= 5 ? "Ace" : kills.Length == 4 ? "4K" : "3K";
            var score = kills.Length >= 5 ? 100 : kills.Length == 4 ? 86 : 68;
            candidates.Add(new CounterStrikeHighlightCandidate(
                group.Key.KillerSteamId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                group.Key.KillerName,
                group.Key.RoundNumber,
                type,
                $"{group.Key.KillerName} erzielt {type}",
                score,
                kills[0].Tick,
                kills[^1].Tick));
        }

        return candidates;
    }
}

public sealed class ClutchHighlightRule : IHighlightRule
{
    public IReadOnlyCollection<CounterStrikeHighlightCandidate> Evaluate(
        CounterStrikeHighlightContext context) =>
        context.Match.Clutches
            .Where(clutch => clutch.HasWon && clutch.OpponentCount >= 2)
            .Select(clutch => new CounterStrikeHighlightCandidate(
                clutch.ClutcherSteamId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                clutch.ClutcherName,
                clutch.RoundNumber,
                $"1v{clutch.OpponentCount}",
                $"{clutch.ClutcherName} gewinnt 1v{clutch.OpponentCount}",
                Math.Min(100, 66 + clutch.OpponentCount * 7),
                clutch.Tick))
            .ToArray();
}

public sealed class SpecialKillHighlightRule : IHighlightRule
{
    public IReadOnlyCollection<CounterStrikeHighlightCandidate> Evaluate(
        CounterStrikeHighlightContext context)
    {
        var result = new List<CounterStrikeHighlightCandidate>();
        foreach (var kill in context.Match.Kills.Where(kill => kill.KillerSteamId != 0))
        {
            var type = kill.IsNoScope
                ? "No-Scope"
                : kill.IsThroughSmoke
                    ? "Smoke Kill"
                    : kill.PenetratedObjects > 0
                        ? "Wallbang"
                        : kill.WeaponName.Contains("knife", StringComparison.OrdinalIgnoreCase)
                            || kill.WeaponName.Contains("bayonet", StringComparison.OrdinalIgnoreCase)
                            ? "Knife Kill"
                            : null;
            if (type is null)
            {
                continue;
            }

            result.Add(new CounterStrikeHighlightCandidate(
                kill.KillerSteamId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                kill.KillerName,
                kill.RoundNumber,
                type,
                $"{kill.KillerName}: {type}",
                type == "Knife Kill" ? 74 : 58,
                kill.Tick));
        }

        return result;
    }
}

public sealed class NinjaDefuseHighlightRule : IHighlightRule
{
    public IReadOnlyCollection<CounterStrikeHighlightCandidate> Evaluate(
        CounterStrikeHighlightContext context)
    {
        var winningClutches = context.Match.Clutches
            .Where(clutch => clutch.HasWon && clutch.OpponentCount >= 1)
            .ToDictionary(clutch => (clutch.RoundNumber, clutch.ClutcherSteamId));
        return context.Match.BombsDefused
            .Where(defuse => winningClutches.ContainsKey((defuse.RoundNumber, defuse.DefuserSteamId)))
            .Select(defuse => new CounterStrikeHighlightCandidate(
                defuse.DefuserSteamId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                defuse.DefuserName,
                defuse.RoundNumber,
                "Ninja Defuse",
                $"{defuse.DefuserName} rettet die Runde mit einem Ninja Defuse",
                92,
                defuse.Tick))
            .ToArray();
    }
}
