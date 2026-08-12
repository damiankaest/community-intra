using CommunityIntranet.Modules.Mystery.Domain;

namespace CommunityIntranet.Modules.Mystery.Contracts;

public sealed record CreateMysterySessionRequest(
    string[] Players,
    int DurationMinutes,
    MysteryDifficulty Difficulty,
    string Genre,
    string Atmosphere,
    MysteryLocationInput[]? Locations,
    string[]? AvailableItems);

public sealed record MysteryLocationInput(
    string Id,
    string Description,
    double AvailableFromProgress,
    string PreferredUse);

public sealed record MysteryVersionRequest(Guid? Version);

public sealed record SubmitMysteryPuzzleRequest(string Answer, Guid? Version);

public sealed record SubmitMysteryDecisionRequest(string ChoiceId, Guid? Version);

public sealed record RequestMysteryHintRequest(int Level, Guid? Version);

public sealed record AskMysteryQuestionRequest(string Question, Guid? Version);

public sealed record UpdateMysteryNotesRequest(string[] Notes, Guid? Version);

public sealed record SubmitMysteryFinaleRequest(
    string CulpritId,
    string Motive,
    string? Sequence,
    Guid? Version);

public sealed record MysterySessionResponse(
    Guid Id,
    string JoinCode,
    string Title,
    MysteryGameStatus Status,
    string GameMaster,
    string? Notice,
    Guid Version,
    int Chapter,
    string Phase,
    string[] Players,
    MysteryDifficulty Difficulty,
    int DurationMinutes,
    string Genre,
    string Atmosphere,
    MysterySceneResponse? CurrentScene,
    MysteryEvidenceResponse[] Evidence,
    MysteryPuzzleArchiveResponse[] Puzzles,
    MysteryCharacterResponse[] Characters,
    MysteryDecisionResponse[] Decisions,
    int SolvedPuzzleCount,
    int UsedHintCount,
    string[] VisitedLocations,
    string[] Notes,
    MysteryQuestionResponse[] Questions,
    MysteryFinaleResponse? Finale);

public sealed record MysterySceneResponse(
    string Id,
    int Chapter,
    bool IsOpening,
    MysterySceneKind Kind,
    string Title,
    string Narrative,
    string? Prompt,
    MysteryPuzzleResponse? Puzzle,
    MysteryChoiceResponse[] Choices,
    string? LocationId,
    bool CanAdvance);

public sealed record MysteryPuzzleResponse(
    string Id,
    string Prompt,
    string InputType,
    bool IsSolved);

public sealed record MysteryPuzzleArchiveResponse(
    string Id,
    string SceneTitle,
    int Chapter,
    string Prompt,
    bool IsSolved);

public sealed record MysteryChoiceResponse(string Id, string Label);

public sealed record MysteryEvidenceResponse(string Id, string Title, string Description);

public sealed record MysteryCharacterResponse(
    string Id,
    string Name,
    string Role,
    string Description);

public sealed record MysteryDecisionResponse(
    string SceneId,
    string ChoiceId,
    string ChoiceLabel);

public sealed record MysteryQuestionResponse(
    string Question,
    string Answer,
    DateTimeOffset AskedAt);

public sealed record MysteryFinaleResponse(
    bool CorrectCulprit,
    string CulpritId,
    string CulpritName,
    string Motive,
    string Timeline,
    string Resolution,
    string[] RedHerrings,
    int UsedHints,
    int Score);

public sealed record MysteryHintResponse(int Level, string Hint, MysterySessionResponse Session);

public sealed record MysteryPuzzleResultResponse(
    bool Correct,
    string Message,
    MysterySessionResponse Session);

public sealed record MysteryQuestionAnswerResponse(
    string Answer,
    MysterySessionResponse Session);
