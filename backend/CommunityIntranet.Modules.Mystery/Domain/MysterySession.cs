namespace CommunityIntranet.Modules.Mystery.Domain;

public sealed class MysterySession
{
    public Guid Id { get; set; }

    public string JoinCode { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public MysteryGameStatus Status { get; set; }

    public string GameMaster { get; set; } = string.Empty;

    public string? Notice { get; set; }

    public string ConfigurationJson { get; set; } = string.Empty;

    public string SecretCaseJson { get; set; } = string.Empty;

    public string GameStateJson { get; set; } = string.Empty;

    public Guid Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public enum MysteryGameStatus
{
    Active,
    ReadyForFinale,
    Completed
}
