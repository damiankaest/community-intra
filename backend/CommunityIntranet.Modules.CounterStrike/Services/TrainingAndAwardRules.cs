using CommunityIntranet.Modules.CounterStrike.Domain;

namespace CommunityIntranet.Modules.CounterStrike.Services;

public sealed record CounterStrikeTrainingRecommendation(
    string Key,
    string Title,
    string Reason,
    CounterStrikeTrainingKind Kind,
    int Priority,
    string Route);

public interface ITrainingRecommendationRule
{
    CounterStrikeTrainingRecommendation? Evaluate(CounterStrikePlayerStats stats);
}

public sealed class UtilityTrainingRecommendationRule : ITrainingRecommendationRule
{
    public CounterStrikeTrainingRecommendation? Evaluate(CounterStrikePlayerStats stats)
    {
        var perMatch = stats.Matches == 0 ? 0 : (double)stats.UtilityDamage / stats.Matches;
        return stats.Matches >= 2 && perMatch < 18
            ? new CounterStrikeTrainingRecommendation(
                "utility-low",
                "Utility mit Plan einsetzen",
                $"Du verursachst aktuell nur {perMatch:F0} Utility-Schaden pro Match.",
                CounterStrikeTrainingKind.Utility,
                90,
                "utility")
            : null;
    }
}

public sealed class FirstDuelTrainingRecommendationRule : ITrainingRecommendationRule
{
    public CounterStrikeTrainingRecommendation? Evaluate(CounterStrikePlayerStats stats) =>
        stats.Matches >= 2 && stats.FirstDeaths > stats.FirstKills
            ? new CounterStrikeTrainingRecommendation(
                "first-duels",
                "Ersten Kontakt stabilisieren",
                $"Deine First Duels stehen bei {stats.FirstKills}:{stats.FirstDeaths}.",
                CounterStrikeTrainingKind.Reaction,
                85,
                "aim?mode=reaction")
            : null;
}

public sealed class PrecisionTrainingRecommendationRule : ITrainingRecommendationRule
{
    public CounterStrikeTrainingRecommendation? Evaluate(CounterStrikePlayerStats stats) =>
        stats.Matches >= 2 && stats.HeadshotPercent < 32
            ? new CounterStrikeTrainingRecommendation(
                "precision",
                "Crosshair Placement schärfen",
                $"Deine Headshot-Quote liegt bei {stats.HeadshotPercent:F0} %.",
                CounterStrikeTrainingKind.Flick,
                70,
                "aim?mode=flick")
            : null;
}

public sealed class TradingTrainingRecommendationRule : ITrainingRecommendationRule
{
    public CounterStrikeTrainingRecommendation? Evaluate(CounterStrikePlayerStats stats) =>
        stats.Matches >= 3 && stats.TradeKills < stats.Matches
            ? new CounterStrikeTrainingRecommendation(
                "trading",
                "Abstände und Trades verbessern",
                "Du tradest im Schnitt weniger als einen Mitspieler pro Match.",
                CounterStrikeTrainingKind.Teamplay,
                65,
                "")
            : null;
}

public sealed record CounterStrikeAwardCandidate(
    string Key,
    string Name,
    string Description,
    string Icon,
    Guid UserId,
    double Value);

public interface ICounterStrikeAwardRule
{
    CounterStrikeAwardCandidate? Evaluate(IReadOnlyCollection<CounterStrikePlayerStats> stats);
}

public sealed class MvpAwardRule : ICounterStrikeAwardRule
{
    public CounterStrikeAwardCandidate? Evaluate(IReadOnlyCollection<CounterStrikePlayerStats> stats)
    {
        var winner = stats.Where(item => item.Matches > 0).MaxBy(item => item.HltvRating);
        return winner is null ? null : new CounterStrikeAwardCandidate(
            "mvp", "MVP", "Höchstes HLTV Rating der Season", "🏆", winner.UserId, winner.HltvRating);
    }
}

public sealed class EntryKingAwardRule : ICounterStrikeAwardRule
{
    public CounterStrikeAwardCandidate? Evaluate(IReadOnlyCollection<CounterStrikePlayerStats> stats)
    {
        var winner = stats.Where(item => item.Matches > 0).MaxBy(item => item.FirstKills - item.FirstDeaths);
        return winner is null ? null : new CounterStrikeAwardCandidate(
            "entry-king", "Entry King", "Beste First-Kill-Differenz", "⚡", winner.UserId,
            winner.FirstKills - winner.FirstDeaths);
    }
}

public sealed class ClutchKingAwardRule : ICounterStrikeAwardRule
{
    public CounterStrikeAwardCandidate? Evaluate(IReadOnlyCollection<CounterStrikePlayerStats> stats)
    {
        var winner = stats.Where(item => item.ClutchesWon > 0).MaxBy(item => item.ClutchesWon);
        return winner is null ? null : new CounterStrikeAwardCandidate(
            "clutch-king", "Clutch King", "Die meisten gewonnenen 1vX-Situationen", "🧊", winner.UserId,
            winner.ClutchesWon);
    }
}

public sealed class HeadshotKingAwardRule : ICounterStrikeAwardRule
{
    public CounterStrikeAwardCandidate? Evaluate(IReadOnlyCollection<CounterStrikePlayerStats> stats)
    {
        var winner = stats.Where(item => item.Matches >= 2).MaxBy(item => item.HeadshotPercent);
        return winner is null ? null : new CounterStrikeAwardCandidate(
            "headshot-king", "Headshot King", "Höchste Headshot-Quote", "🎯", winner.UserId,
            winner.HeadshotPercent);
    }
}

public sealed class UtilityMasterAwardRule : ICounterStrikeAwardRule
{
    public CounterStrikeAwardCandidate? Evaluate(IReadOnlyCollection<CounterStrikePlayerStats> stats)
    {
        var winner = stats.Where(item => item.Matches > 0).MaxBy(item => item.UtilityDamage);
        return winner is null ? null : new CounterStrikeAwardCandidate(
            "utility-master", "Utility Master", "Meister der Granaten", "💥", winner.UserId,
            winner.UtilityDamage);
    }
}

public sealed class DeathCollectorAwardRule : ICounterStrikeAwardRule
{
    public CounterStrikeAwardCandidate? Evaluate(IReadOnlyCollection<CounterStrikePlayerStats> stats)
    {
        var winner = stats.Where(item => item.Matches > 0).MaxBy(item => item.Deaths);
        return winner is null ? null : new CounterStrikeAwardCandidate(
            "death-collector", "Death Collector", "Hat wirklich jeden Respawn mitgenommen", "💀", winner.UserId,
            winner.Deaths);
    }
}
