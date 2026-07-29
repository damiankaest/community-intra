using CommunityIntranet.Modules.AiAssistant.Contracts;
using CommunityIntranet.Modules.AiAssistant.Domain;
using CommunityIntranet.Modules.ThemePacks.Configuration;

namespace CommunityIntranet.Modules.AiAssistant.Services;

public interface IWorkspaceChatGenerator
{
    bool IsConfigured { get; }

    string Model { get; }

    IAsyncEnumerable<WorkspaceChatEvent> StreamAsync(
        Guid organizationId,
        Guid memberId,
        Guid conversationId,
        AssistantTone tone,
        ThemePackConfiguration theme,
        IReadOnlyList<AssistantMessage> messages,
        bool canCreateContent,
        CancellationToken cancellationToken);
}

public sealed record WorkspaceChatEvent(
    string? Delta = null,
    AssistantAction? Action = null);
