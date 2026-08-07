namespace CommunityIntranet.Modules.Parties.Domain;

public enum PartyMusicRequestStatus
{
    Open,
    Played,
    Rejected
}

public sealed class PartyMusicRequest
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid GuestId { get; set; }
    public required string Song { get; set; }
    public string? Artist { get; set; }
    public string? Comment { get; set; }
    public PartyMusicRequestStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
