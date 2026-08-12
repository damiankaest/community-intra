using System.Globalization;
using CommunityIntranet.Modules.Mystery.Domain;

namespace CommunityIntranet.Modules.Mystery.Game;

public static class MysteryGameEngine
{
    public static MysteryGameState CreateInitialState(MysteryCaseDefinition mysteryCase)
    {
        var state = new MysteryGameState();
        RevealCurrentScene(mysteryCase, state);
        return state;
    }

    public static MysteryActionResult Advance(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state)
    {
        if (state.Status != MysteryGameStatus.Active)
        {
            return MysteryActionResult.Failure("Das Spiel kann gerade nicht fortgesetzt werden.");
        }

        var scene = CurrentScene(mysteryCase, state);
        if (scene.PuzzleId is not null && !state.SolvedPuzzleIds.Contains(scene.PuzzleId))
        {
            return MysteryActionResult.Failure("Löst zuerst das aktuelle Rätsel.");
        }

        if (scene.Choices.Length > 0 && !state.Decisions.ContainsKey(scene.Id))
        {
            return MysteryActionResult.Failure("Trefft zuerst eine gemeinsame Entscheidung.");
        }

        if (state.CurrentSceneIndex >= mysteryCase.Scenes.Length - 1)
        {
            state.Status = MysteryGameStatus.ReadyForFinale;
            return MysteryActionResult.Success();
        }

        state.CurrentSceneIndex++;
        RevealCurrentScene(mysteryCase, state);
        return MysteryActionResult.Success();
    }

    public static MysteryPuzzleResult SubmitPuzzle(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state,
        string answer)
    {
        if (state.Status != MysteryGameStatus.Active)
        {
            return MysteryPuzzleResult.Failure("Aktuell ist kein Rätsel aktiv.");
        }

        var scene = CurrentScene(mysteryCase, state);
        var puzzle = mysteryCase.Puzzles.SingleOrDefault(x => x.Id == scene.PuzzleId);
        if (puzzle is null)
        {
            return MysteryPuzzleResult.Failure("Diese Szene enthält kein Rätsel.");
        }

        if (state.SolvedPuzzleIds.Contains(puzzle.Id))
        {
            return MysteryPuzzleResult.Correct("Dieses Rätsel habt ihr bereits gelöst.");
        }

        var normalized = NormalizeAnswer(answer);
        var isCorrect = normalized.Length > 0
            && puzzle.AcceptedAnswers.Any(candidate => NormalizeAnswer(candidate) == normalized);
        if (!isCorrect)
        {
            state.InvalidPuzzleAttempts++;
            return MysteryPuzzleResult.Failure("Das passt noch nicht. Prüft eure Spuren noch einmal.");
        }

        state.SolvedPuzzleIds.Add(puzzle.Id);
        state.StoryFlags.Add($"puzzle:{puzzle.Id}:solved");
        return MysteryPuzzleResult.Correct("Richtig. Das Schloss gibt nach und eine neue Spur wird sichtbar.");
    }

    public static MysteryActionResult Choose(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state,
        string choiceId)
    {
        if (state.Status != MysteryGameStatus.Active)
        {
            return MysteryActionResult.Failure("Aktuell ist keine Entscheidung möglich.");
        }

        var scene = CurrentScene(mysteryCase, state);
        var choice = scene.Choices.SingleOrDefault(x => x.Id == choiceId);
        if (choice is null)
        {
            return MysteryActionResult.Failure("Diese Entscheidung ist nicht verfügbar.");
        }

        state.Decisions[scene.Id] = choice.Id;
        foreach (var flag in choice.StoryFlags)
        {
            state.StoryFlags.Add(flag);
        }

        return MysteryActionResult.Success();
    }

    public static string GetHint(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state,
        int level,
        DateTimeOffset now)
    {
        if (state.Status != MysteryGameStatus.Active || level is < 1 or > 3)
        {
            throw new InvalidOperationException("Diese Hinweisstufe ist nicht verfügbar.");
        }

        var scene = CurrentScene(mysteryCase, state);
        var puzzle = mysteryCase.Puzzles.SingleOrDefault(x => x.Id == scene.PuzzleId);
        var hints = puzzle?.Hints ?? scene.Hints;
        state.UsedHints.Add(new MysteryHintUsage
        {
            SceneId = scene.Id,
            Level = level,
            UsedAt = now
        });
        return hints[level - 1];
    }

    public static MysteryActionResult Complete(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state,
        MysteryFinalTheory theory)
    {
        if (state.Status != MysteryGameStatus.ReadyForFinale)
        {
            return MysteryActionResult.Failure("Die Theorie kann erst nach der letzten Szene abgegeben werden.");
        }

        if (!mysteryCase.Suspects.Any(x => x.Id == theory.CulpritId))
        {
            return MysteryActionResult.Failure("Wählt eine bekannte verdächtige Person aus.");
        }

        state.FinalTheory = theory;
        state.Status = MysteryGameStatus.Completed;
        return MysteryActionResult.Success();
    }

    public static bool CanAdvance(MysteryCaseDefinition mysteryCase, MysteryGameState state)
    {
        if (state.Status != MysteryGameStatus.Active)
        {
            return false;
        }

        var scene = CurrentScene(mysteryCase, state);
        return (scene.PuzzleId is null || state.SolvedPuzzleIds.Contains(scene.PuzzleId))
            && (scene.Choices.Length == 0 || state.Decisions.ContainsKey(scene.Id));
    }

    private static MysterySceneDefinition CurrentScene(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state) => mysteryCase.Scenes[state.CurrentSceneIndex];

    private static void RevealCurrentScene(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state)
    {
        var scene = CurrentScene(mysteryCase, state);
        state.FoundEvidenceIds.UnionWith(scene.EvidenceIds);
        state.KnownCharacterIds.UnionWith(scene.CharacterIds);
        state.StoryFlags.UnionWith(scene.StoryFlags);
        if (scene.LocationId is not null)
        {
            state.VisitedLocationIds.Add(scene.LocationId);
        }
    }

    internal static string NormalizeAnswer(string value) => string.Concat(
        value.Trim().ToLower(CultureInfo.InvariantCulture)
            .Where(char.IsLetterOrDigit));
}

public sealed record MysteryActionResult(bool IsSuccess, string? Error)
{
    public static MysteryActionResult Success() => new(true, null);

    public static MysteryActionResult Failure(string error) => new(false, error);
}

public sealed record MysteryPuzzleResult(bool IsCorrect, string Message)
{
    public static MysteryPuzzleResult Correct(string message) => new(true, message);

    public static MysteryPuzzleResult Failure(string message) => new(false, message);
}
