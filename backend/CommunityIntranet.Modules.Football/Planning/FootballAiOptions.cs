namespace CommunityIntranet.Modules.Football.Planning;

public sealed class FootballAiOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5.6";

    public Uri Endpoint { get; set; } = new("https://api.openai.com/v1/responses");
}
