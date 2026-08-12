using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommunityIntranet.Modules.Mystery.Domain;

public static class MysteryJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}

public sealed class MysteryGameConfiguration
{
    public string[] Players { get; set; } = [];

    public int DurationMinutes { get; set; }

    public MysteryDifficulty Difficulty { get; set; }

    public string Genre { get; set; } = string.Empty;

    public string Atmosphere { get; set; } = string.Empty;

    public MysteryLocationOption[] Locations { get; set; } = [];

    public string[] AvailableItems { get; set; } = [];
}

public enum MysteryDifficulty
{
    Easy,
    Medium,
    Hard
}

public sealed class MysteryLocationOption
{
    public string Id { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public double AvailableFromProgress { get; set; }

    public string PreferredUse { get; set; } = string.Empty;
}

public sealed class MysteryCaseDefinition
{
    public string Title { get; set; } = string.Empty;

    public string Opening { get; set; } = string.Empty;

    public string Victim { get; set; } = string.Empty;

    public string CulpritId { get; set; } = string.Empty;

    public string Motive { get; set; } = string.Empty;

    public string Timeline { get; set; } = string.Empty;

    public MysteryCharacterDefinition[] Suspects { get; set; } = [];

    public MysteryEvidenceDefinition[] Evidence { get; set; } = [];

    public MysteryPuzzleDefinition[] Puzzles { get; set; } = [];

    public MysterySceneDefinition[] Scenes { get; set; } = [];

    public string Resolution { get; set; } = string.Empty;
}

public sealed class MysteryCharacterDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string PublicDescription { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;
}

public sealed class MysteryEvidenceDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsRedHerring { get; set; }
}

public sealed class MysteryPuzzleDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Prompt { get; set; } = string.Empty;

    public string InputType { get; set; } = "text";

    public string Solution { get; set; } = string.Empty;

    public string[] AcceptedAnswers { get; set; } = [];

    public string[] Hints { get; set; } = [];
}

public sealed class MysterySceneDefinition
{
    public string Id { get; set; } = string.Empty;

    public int Chapter { get; set; }

    public MysterySceneKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Narrative { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public string[] EvidenceIds { get; set; } = [];

    public string[] CharacterIds { get; set; } = [];

    public string? PuzzleId { get; set; }

    public MysteryChoiceDefinition[] Choices { get; set; } = [];

    public string? LocationId { get; set; }

    public string[] StoryFlags { get; set; } = [];

    public string[] Hints { get; set; } = [];
}

public enum MysterySceneKind
{
    Story,
    Dialogue,
    Evidence,
    Puzzle,
    Decision,
    RealTask,
    LocationChange
}

public sealed class MysteryChoiceDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Consequence { get; set; } = string.Empty;

    public string[] StoryFlags { get; set; } = [];
}

public sealed class MysteryGameState
{
    public MysteryGameStatus Status { get; set; } = MysteryGameStatus.Active;

    public int CurrentSceneIndex { get; set; }

    public HashSet<string> FoundEvidenceIds { get; set; } = [];

    public HashSet<string> KnownCharacterIds { get; set; } = [];

    public Dictionary<string, string> Decisions { get; set; } = [];

    public HashSet<string> SolvedPuzzleIds { get; set; } = [];

    public List<MysteryHintUsage> UsedHints { get; set; } = [];

    public HashSet<string> VisitedLocationIds { get; set; } = [];

    public List<string> Notes { get; set; } = [];

    public HashSet<string> StoryFlags { get; set; } = [];

    public List<MysteryQuestionAnswer> Questions { get; set; } = [];

    public int InvalidPuzzleAttempts { get; set; }

    public MysteryFinalTheory? FinalTheory { get; set; }
}

public sealed class MysteryHintUsage
{
    public string SceneId { get; set; } = string.Empty;

    public int Level { get; set; }

    public DateTimeOffset UsedAt { get; set; }
}

public sealed class MysteryQuestionAnswer
{
    public string Question { get; set; } = string.Empty;

    public string Answer { get; set; } = string.Empty;

    public DateTimeOffset AskedAt { get; set; }
}

public sealed class MysteryFinalTheory
{
    public string CulpritId { get; set; } = string.Empty;

    public string Motive { get; set; } = string.Empty;

    public string? Sequence { get; set; }
}
