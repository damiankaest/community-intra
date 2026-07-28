namespace CommunityIntranet.Modules.ThemePacks.Domain;

public sealed class ThemePack
{
    public Guid Id { get; set; }

    public required string Key { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Version { get; set; }

    public required string Author { get; set; }

    public bool IsSystemTheme { get; set; }

    public required string ConfigurationJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
