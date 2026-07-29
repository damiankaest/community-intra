namespace CommunityIntranet.Modules.AiAssistant.Services;

public sealed class AiAssistantOptions
{
    public const string SectionName = "AiAssistant";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5.6";

    public Uri Endpoint { get; set; } =
        new("https://api.openai.com/v1/responses");

    public int DraftLifetimeMinutes { get; set; } = 30;
}
