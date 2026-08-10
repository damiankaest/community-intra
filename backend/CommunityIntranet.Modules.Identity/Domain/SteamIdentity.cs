namespace CommunityIntranet.Modules.Identity.Domain;

public sealed class SteamIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string SteamId64 { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTimeOffset LinkedAt { get; set; }
    public DateTimeOffset? ProfileUpdatedAt { get; set; }
}
