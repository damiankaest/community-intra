namespace CommunityIntranet.Modules.Parties.Domain;

public sealed class Party
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public required string Type { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public string? WelcomeText { get; set; }
    public bool IsActive { get; set; }
    public bool IsArchived { get; set; }
    public bool GuestsCanViewGallery { get; set; } = true;
    public bool GuestsCanViewGuestbook { get; set; } = true;
    public string? SpotifyProtectedRefreshToken { get; set; }
    public string? SpotifyAccountName { get; set; }
    public DateTimeOffset? SpotifyConnectedAt { get; set; }
    public bool SpotifyAutoQueue { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
