namespace CommunityIntranet.Modules.Parties.Domain;

public sealed class PartyGuestbookEntry
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid GuestId { get; set; }
    public required string Message { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
