using CommunityIntranet.Modules.Mystery.Domain;

namespace CommunityIntranet.Modules.Mystery.Game;

public static class MysteryCaseGuard
{
    public static MysteryCaseDefinition ValidateAndNormalize(
        MysteryCaseDefinition mysteryCase,
        MysteryGameConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(mysteryCase.Title)
            || string.IsNullOrWhiteSpace(mysteryCase.CulpritId)
            || string.IsNullOrWhiteSpace(mysteryCase.Motive)
            || string.IsNullOrWhiteSpace(mysteryCase.Resolution))
        {
            throw new InvalidOperationException("Der erzeugte Fall ist unvollständig.");
        }

        if (mysteryCase.Suspects is not { Length: >= 3 }
            || mysteryCase.Scenes is not { Length: >= 4 and <= 12 }
            || mysteryCase.Evidence is not { Length: >= 3 })
        {
            throw new InvalidOperationException("Der erzeugte Fall besitzt nicht genug spielbare Inhalte.");
        }

        EnsureUnique(mysteryCase.Suspects.Select(x => x.Id), "Charaktere");
        EnsureUnique(mysteryCase.Evidence.Select(x => x.Id), "Beweise");
        EnsureUnique(mysteryCase.Puzzles.Select(x => x.Id), "Rätsel");
        EnsureUnique(mysteryCase.Scenes.Select(x => x.Id), "Szenen");

        var minimumPuzzleCount = configuration.Difficulty switch
        {
            MysteryDifficulty.Easy => 1,
            MysteryDifficulty.Medium => 2,
            MysteryDifficulty.Hard => 3,
            _ => 1
        };
        var minimumSceneCount = configuration.Difficulty switch
        {
            MysteryDifficulty.Easy => 6,
            MysteryDifficulty.Medium => 8,
            MysteryDifficulty.Hard => 9,
            _ => 6
        };
        if (mysteryCase.Scenes.Length < minimumSceneCount)
        {
            throw new InvalidOperationException(
                $"Für {configuration.Difficulty} werden mindestens {minimumSceneCount} Szenen benötigt.");
        }

        if (mysteryCase.Puzzles.Length < minimumPuzzleCount)
        {
            throw new InvalidOperationException(
                $"Für {configuration.Difficulty} werden mindestens {minimumPuzzleCount} Rätsel benötigt.");
        }

        var characterIds = mysteryCase.Suspects.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var evidenceIds = mysteryCase.Evidence.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var puzzleIds = mysteryCase.Puzzles.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var locations = configuration.Locations.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var introducedCharacterIds = new HashSet<string>(StringComparer.Ordinal);

        if (!characterIds.Contains(mysteryCase.CulpritId))
        {
            throw new InvalidOperationException("Der Täter ist kein definierter Verdächtiger.");
        }

        for (var index = 0; index < mysteryCase.Scenes.Length; index++)
        {
            var scene = mysteryCase.Scenes[index];
            if (string.IsNullOrWhiteSpace(scene.Id)
                || string.IsNullOrWhiteSpace(scene.Title)
                || string.IsNullOrWhiteSpace(scene.Narrative))
            {
                throw new InvalidOperationException("Eine erzeugte Szene ist unvollständig.");
            }

            if (scene.EvidenceIds.Any(id => !evidenceIds.Contains(id))
                || scene.CharacterIds.Any(id => !characterIds.Contains(id))
                || scene.PuzzleId is not null && !puzzleIds.Contains(scene.PuzzleId))
            {
                throw new InvalidOperationException("Eine Szene verweist auf unbekannte Falldaten.");
            }

            var newlyIntroducedCharacterIds = scene.CharacterIds
                .Where(id => !introducedCharacterIds.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (newlyIntroducedCharacterIds.Length > 1)
            {
                throw new InvalidOperationException(
                    "Eine Szene führt mehr als eine neue verdächtige Person gleichzeitig ein.");
            }

            introducedCharacterIds.UnionWith(newlyIntroducedCharacterIds);

            if (scene.LocationId is not null)
            {
                if (!locations.TryGetValue(scene.LocationId, out var location))
                {
                    throw new InvalidOperationException("Eine Szene verweist auf einen unbekannten realen Ort.");
                }

                var progress = (double)index / Math.Max(1, mysteryCase.Scenes.Length - 1);
                if (progress + 0.001 < location.AvailableFromProgress)
                {
                    throw new InvalidOperationException("Ein realer Ort wird zu früh in den Fall eingebaut.");
                }
            }

            scene.Hints = NormalizeHints(scene.Hints, scene.Prompt ?? scene.Narrative);
        }

        if (!introducedCharacterIds.SetEquals(characterIds))
        {
            throw new InvalidOperationException(
                "Nicht alle Verdächtigen werden im Verlauf des Falls vorgestellt.");
        }

        foreach (var puzzle in mysteryCase.Puzzles)
        {
            if (string.IsNullOrWhiteSpace(puzzle.Id)
                || string.IsNullOrWhiteSpace(puzzle.Prompt)
                || string.IsNullOrWhiteSpace(puzzle.Solution))
            {
                throw new InvalidOperationException("Ein erzeugtes Rätsel ist unvollständig.");
            }

            puzzle.InputType = puzzle.InputType is "code" or "text" ? puzzle.InputType : "text";
            puzzle.AcceptedAnswers = puzzle.AcceptedAnswers
                .Append(puzzle.Solution)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();
            puzzle.Hints = NormalizeHints(puzzle.Hints, puzzle.Prompt);

            var puzzleSceneIndex = Array.FindIndex(
                mysteryCase.Scenes,
                scene => scene.PuzzleId == puzzle.Id);
            if (puzzleSceneIndex < 0)
            {
                throw new InvalidOperationException("Ein erzeugtes Rätsel wird in keiner Szene verwendet.");
            }

            var normalizedSolution = MysteryGameEngine.NormalizeAnswer(puzzle.Solution);
            if (normalizedSolution.Length >= 3)
            {
                var visibleEvidenceIds = mysteryCase.Scenes
                    .Take(puzzleSceneIndex + 1)
                    .SelectMany(scene => scene.EvidenceIds)
                    .ToHashSet(StringComparer.Ordinal);
                var solutionIsCopiedFromEvidence = mysteryCase.Evidence
                    .Where(evidence => visibleEvidenceIds.Contains(evidence.Id))
                    .Any(evidence => MysteryGameEngine.NormalizeAnswer(evidence.Description)
                        .Contains(normalizedSolution, StringComparison.Ordinal));
                if (solutionIsCopiedFromEvidence)
                {
                    throw new InvalidOperationException(
                        "Eine Rätsellösung wird bereits direkt in einer sichtbaren Spur verraten.");
                }
            }
        }

        mysteryCase.Title = mysteryCase.Title.Trim()[..Math.Min(180, mysteryCase.Title.Trim().Length)];
        return mysteryCase;
    }

    private static void EnsureUnique(IEnumerable<string> ids, string label)
    {
        var values = ids.ToArray();
        if (values.Any(string.IsNullOrWhiteSpace)
            || values.Distinct(StringComparer.Ordinal).Count() != values.Length)
        {
            throw new InvalidOperationException($"{label} besitzen ungültige oder doppelte IDs.");
        }
    }

    private static string[] NormalizeHints(string[] hints, string context)
    {
        var normalized = hints
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Take(3)
            .ToList();

        string[] fallbacks =
        [
            "Konzentriert euch auf das Detail, das nicht zum übrigen Ablauf passt.",
            $"Vergleicht alle bisher bekannten Spuren direkt mit dieser Aufgabe: {context}",
            "Geht die Hinweise chronologisch durch und prüft jede Annahme einzeln."
        ];
        while (normalized.Count < 3)
        {
            normalized.Add(fallbacks[normalized.Count]);
        }

        return normalized.ToArray();
    }
}
