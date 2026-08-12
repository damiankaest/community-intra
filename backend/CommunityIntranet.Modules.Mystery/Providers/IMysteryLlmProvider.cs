using CommunityIntranet.Modules.Mystery.Domain;

namespace CommunityIntranet.Modules.Mystery.Providers;

public interface IMysteryLlmProvider
{
    Task<MysteryCaseGenerationResult> GenerateCaseAsync(
        MysteryGameConfiguration configuration,
        CancellationToken cancellationToken);

    Task<string> AnswerPlayerQuestionAsync(
        MysteryCaseDefinition mysteryCase,
        MysteryGameState state,
        string question,
        CancellationToken cancellationToken);
}

public sealed record MysteryCaseGenerationResult(
    MysteryCaseDefinition Case,
    string ProviderName,
    string? Notice);
