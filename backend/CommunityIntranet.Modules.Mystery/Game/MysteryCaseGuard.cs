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

        var characterIds = mysteryCase.Suspects.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var evidenceIds = mysteryCase.Evidence.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var puzzleIds = mysteryCase.Puzzles.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        var locations = configuration.Locations.ToDictionary(x => x.Id, StringComparer.Ordinal);

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
