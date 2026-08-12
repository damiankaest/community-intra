using System.Text.Json;
using CommunityIntranet.Modules.Mystery.Contracts;
using CommunityIntranet.Modules.Mystery.Domain;

namespace CommunityIntranet.Modules.Mystery.Game;

public static class MysterySessionMapper
{
    public static MysterySessionResponse Map(MysterySession session)
    {
        var configuration = Deserialize<MysteryGameConfiguration>(session.ConfigurationJson);
        var mysteryCase = Deserialize<MysteryCaseDefinition>(session.SecretCaseJson);
        var state = Deserialize<MysteryGameState>(session.GameStateJson);
        var currentScene = state.Status == MysteryGameStatus.Active
            ? mysteryCase.Scenes[state.CurrentSceneIndex]
            : null;
        var puzzle = currentScene?.PuzzleId is null
            ? null
            : mysteryCase.Puzzles.Single(x => x.Id == currentScene.PuzzleId);

        return new MysterySessionResponse(
            session.Id,
            session.JoinCode,
            session.Title,
            state.Status,
            session.GameMaster,
            session.Notice,
            session.Version,
            currentScene?.Chapter ?? mysteryCase.Scenes[^1].Chapter,
            Phase(mysteryCase, state),
            configuration.Players,
            configuration.Difficulty,
            configuration.DurationMinutes,
            configuration.Genre,
            configuration.Atmosphere,
            currentScene is null ? null : new MysterySceneResponse(
                currentScene.Id,
                currentScene.Chapter,
                state.CurrentSceneIndex == 0,
                currentScene.Kind,
                currentScene.Title,
                state.CurrentSceneIndex == 0
                    ? $"{mysteryCase.Opening}\n\n{currentScene.Narrative}"
                    : currentScene.Narrative,
                currentScene.Prompt,
                puzzle is null ? null : new MysteryPuzzleResponse(
                    puzzle.Id,
                    puzzle.Prompt,
                    puzzle.InputType,
                    state.SolvedPuzzleIds.Contains(puzzle.Id)),
                currentScene.Choices.Select(x => new MysteryChoiceResponse(x.Id, x.Label)).ToArray(),
                currentScene.LocationId,
                MysteryGameEngine.CanAdvance(mysteryCase, state)),
            mysteryCase.Evidence
                .Where(x => state.FoundEvidenceIds.Contains(x.Id))
                .Select(x => new MysteryEvidenceResponse(x.Id, x.Title, x.Description))
                .ToArray(),
            mysteryCase.Scenes
                .Take(state.CurrentSceneIndex + 1)
                .Where(x => x.PuzzleId is not null)
                .Select(scene =>
                {
                    var discoveredPuzzle = mysteryCase.Puzzles.Single(x => x.Id == scene.PuzzleId);
                    return new MysteryPuzzleArchiveResponse(
                        discoveredPuzzle.Id,
                        scene.Title,
                        scene.Chapter,
                        discoveredPuzzle.Prompt,
                        state.SolvedPuzzleIds.Contains(discoveredPuzzle.Id));
                })
                .ToArray(),
            mysteryCase.Suspects
                .Where(x => state.KnownCharacterIds.Contains(x.Id))
                .Select(x => new MysteryCharacterResponse(x.Id, x.Name, x.Role, x.PublicDescription))
                .ToArray(),
            state.Decisions.Select(pair =>
            {
                var scene = mysteryCase.Scenes.Single(x => x.Id == pair.Key);
                var choice = scene.Choices.Single(x => x.Id == pair.Value);
                return new MysteryDecisionResponse(scene.Id, choice.Id, choice.Label);
            }).ToArray(),
            state.SolvedPuzzleIds.Count,
            state.UsedHints.Count,
            state.VisitedLocationIds.ToArray(),
            state.Notes.ToArray(),
            state.Questions.TakeLast(8)
                .Select(x => new MysteryQuestionResponse(x.Question, x.Answer, x.AskedAt))
                .ToArray(),
            state.Status == MysteryGameStatus.Completed
                ? MapFinale(mysteryCase, state)
                : null);
    }

    public static T Deserialize<T>(string json) where T : class =>
        JsonSerializer.Deserialize<T>(json, MysteryJson.Options)
        ?? throw new InvalidOperationException("Der gespeicherte Spielzustand ist ungültig.");

    private static string Phase(MysteryCaseDefinition mysteryCase, MysteryGameState state)
    {
        if (state.Status == MysteryGameStatus.Completed) return "Aufgelöst";
        if (state.Status == MysteryGameStatus.ReadyForFinale) return "Zeit für eure Theorie";
        var ratio = (double)state.CurrentSceneIndex / Math.Max(1, mysteryCase.Scenes.Length - 1);
        return ratio switch
        {
            < 0.34 => "Die ersten Spuren",
            < 0.7 => "Die Widersprüche verdichten sich",
            _ => "Kurz vor der Wahrheit"
        };
    }

    private static MysteryFinaleResponse MapFinale(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state)
    {
        var culprit = mysteryCase.Suspects.Single(x => x.Id == mysteryCase.CulpritId);
        var correct = state.FinalTheory?.CulpritId == mysteryCase.CulpritId;
        var score = Math.Max(
            0,
            600 + (correct ? 400 : 0)
            - state.UsedHints.Sum(x => x.Level * 35)
            - state.InvalidPuzzleAttempts * 15);
        return new MysteryFinaleResponse(
            correct,
            culprit.Id,
            culprit.Name,
            mysteryCase.Motive,
            mysteryCase.Timeline,
            mysteryCase.Resolution,
            mysteryCase.Evidence.Where(x => x.IsRedHerring).Select(x => x.Title).ToArray(),
            state.UsedHints.Count,
            score);
    }
}
