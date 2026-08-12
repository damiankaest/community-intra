using CommunityIntranet.Modules.Mystery.Domain;

namespace CommunityIntranet.Modules.Mystery.Game;

public static class MysterySpoilerGuard
{
    private const string SafeFallback =
        "Das würde euch gerade zu viel von der Auflösung vorwegnehmen. Bleibt bei den bereits gefundenen Spuren und formuliert die Frage enger.";
    private static readonly string[] CulpritMarkers =
        ["täter", "täterin", "mörder", "mörderin", "schuldig", "schuldige"];

    public static string ProtectAnswer(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state,
        string answer)
    {
        var trimmed = answer.Trim();
        if (state.Status == MysteryGameStatus.Completed)
        {
            return trimmed;
        }

        var culprit = mysteryCase.Suspects.Single(x => x.Id == mysteryCase.CulpritId);
        if (Contains(trimmed, culprit.Name)
            && CulpritMarkers.Any(marker => Contains(trimmed, marker)))
        {
            return SafeFallback;
        }

        if (Contains(trimmed, mysteryCase.Motive)
            || mysteryCase.Puzzles.Any(puzzle =>
                puzzle.Solution.Length >= 3 && Contains(trimmed, puzzle.Solution)))
        {
            return SafeFallback;
        }

        var futureSceneIds = mysteryCase.Scenes
            .Skip(state.CurrentSceneIndex + 1)
            .Select(x => x.Id)
            .ToHashSet(StringComparer.Ordinal);
        var futureEvidenceIds = mysteryCase.Scenes
            .Where(x => futureSceneIds.Contains(x.Id))
            .SelectMany(x => x.EvidenceIds)
            .Where(id => !state.FoundEvidenceIds.Contains(id))
            .ToHashSet(StringComparer.Ordinal);

        if (mysteryCase.Evidence.Any(evidence =>
                futureEvidenceIds.Contains(evidence.Id)
                && (ContainsMeaningful(trimmed, evidence.Title)
                    || ContainsMeaningful(trimmed, evidence.Description)))
            || mysteryCase.Scenes.Skip(state.CurrentSceneIndex + 1).Any(scene =>
                ContainsMeaningful(trimmed, scene.Title)))
        {
            return SafeFallback;
        }

        return trimmed.Length == 0
            ? "Ich brauche eine etwas konkretere Frage zu euren bisherigen Spuren."
            : trimmed[..Math.Min(trimmed.Length, 1600)];
    }

    private static bool Contains(string text, string value) =>
        !string.IsNullOrWhiteSpace(value)
        && text.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool ContainsMeaningful(string text, string value) =>
        value.Length >= 8 && Contains(text, value);
}
