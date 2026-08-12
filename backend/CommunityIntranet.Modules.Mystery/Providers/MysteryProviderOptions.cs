namespace CommunityIntranet.Modules.Mystery.Providers;

public sealed class MysteryProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "gpt-5.6";

    public bool FallbackOnGenerationError { get; set; }

    public Uri Endpoint { get; set; } = new("https://api.openai.com/v1/responses");
}

public sealed class MysteryGenerationException : Exception
{
    public MysteryGenerationException()
    {
    }

    public MysteryGenerationException(string message) : base(message)
    {
    }

    public MysteryGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
