namespace CommunityIntranet.Modules.Parties.Domain;

public sealed class PartyMedia
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public Guid GuestId { get; set; }
    public required string MediaType { get; set; }
    public required string StoragePath { get; set; }
    public required string FileName { get; set; }
    public required string MimeType { get; set; }
    public long Size { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
