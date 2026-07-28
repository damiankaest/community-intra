namespace CommunityIntranet.Modules.Organizations.Domain;

public sealed class Organization
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public Guid? ThemePackId { get; set; }

    public required List<string> EnabledModules { get; set; }

    public required string Language { get; set; }

    public required string TimeZone { get; set; }

    public Guid OwnerUserId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsArchived { get; set; }
}
