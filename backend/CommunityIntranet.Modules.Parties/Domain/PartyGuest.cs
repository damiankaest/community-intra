namespace CommunityIntranet.Modules.Parties.Domain;

public sealed class PartyGuest
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public required string Name { get; set; }
    public required string SessionTokenHash { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
