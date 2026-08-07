namespace CommunityIntranet.Modules.Parties.Domain;

public sealed class PartyOrderItem
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}
