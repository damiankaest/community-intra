namespace CommunityIntranet.Modules.Parties.Domain;

public enum PartyOrderStatus
{
    Open,
    Done,
    Cancelled
}

public sealed class PartyOrder
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid GuestId { get; set; }
    public Guid? ClaimedByGuestId { get; set; }
    public Guid? OrderItemId { get; set; }
    public string? CustomText { get; set; }
    public PartyOrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClaimedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
