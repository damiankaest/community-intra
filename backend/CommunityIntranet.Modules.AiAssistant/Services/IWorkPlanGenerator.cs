using CommunityIntranet.Modules.AiAssistant.Contracts;
using CommunityIntranet.Modules.ThemePacks.Configuration;

namespace CommunityIntranet.Modules.AiAssistant.Services;

public interface IWorkPlanGenerator
{
    bool IsConfigured { get; }

    string Model { get; }

    Task<WorkPlanGenerationResult> GenerateAsync(
        string prompt,
        AssistantTone tone,
        ThemePackConfiguration theme,
        CancellationToken cancellationToken);
}

public sealed record WorkPlanGenerationResult(
    WorkPlanProposal? Proposal,
    string? Error)
{
    public bool IsSuccess => Proposal is not null;

    public static WorkPlanGenerationResult Success(WorkPlanProposal proposal) =>
        new(proposal, null);

    public static WorkPlanGenerationResult Failure(string error) =>
        new(null, error);
}
