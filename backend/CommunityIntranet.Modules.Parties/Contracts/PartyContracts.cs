using CommunityIntranet.Modules.Parties.Domain;

namespace CommunityIntranet.Modules.Parties.Contracts;

public sealed record CreatePartyRequest(
    string? Name,
    string? Description,
    string? Type,
    string? Location,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    string? WelcomeText,
    bool IsActive = true,
    bool GuestsCanViewGallery = true,
    bool GuestsCanViewGuestbook = true);

public sealed record UpdatePartyRequest(
    string? Name,
    string? Description,
    string? Type,
    string? Location,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    string? WelcomeText,
    bool IsActive,
    bool GuestsCanViewGallery,
    bool GuestsCanViewGuestbook);

public sealed record PartyResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Type,
    string? Location,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    string? WelcomeText,
    bool IsActive,
    bool GuestsCanViewGallery,
    bool GuestsCanViewGuestbook,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int GuestCount,
    int OpenOrderCount,
    IReadOnlyList<PartyOrderItemResponse> OrderItems);

public sealed record PartyPublicResponse(
    string Name,
    string Slug,
    string? Description,
    string Type,
    string? Location,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    string? WelcomeText,
    bool IsActive,
    bool GuestsCanViewGallery,
    bool GuestsCanViewGuestbook,
    IReadOnlyList<PartyOrderItemResponse> OrderItems);

public sealed record RegisterPartyGuestRequest(string? Name);
public sealed record UpdatePartyGuestRequest(string? Name);
public sealed record PartyGuestSessionResponse(Guid GuestId, string Name, string SessionToken);
public sealed record PartyGuestResponse(Guid Id, string Name, DateTimeOffset FirstSeenAt, DateTimeOffset LastSeenAt);

public sealed record UpsertPartyOrderItemRequest(string? Name, string? Icon, int SortOrder, bool IsActive = true);
public sealed record PartyOrderItemResponse(Guid Id, string Name, string? Icon, int SortOrder, bool IsActive);

public sealed record CreatePartyOrderRequest(Guid? OrderItemId, string? CustomText);
public sealed record ChangePartyOrderStatusRequest(PartyOrderStatus Status);
public sealed record PartyOrderResponse(
    Guid Id,
    Guid GuestId,
    string GuestName,
    Guid? ClaimedByGuestId,
    string? ClaimedByGuestName,
    Guid? OrderItemId,
    string? ItemName,
    string? Icon,
    string? CustomText,
    PartyOrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClaimedAt,
    DateTimeOffset? CompletedAt);

public sealed record ClaimPartyOrdersRequest(IReadOnlyList<Guid>? OrderIds);

public sealed record PartyPulseResponse(
    int GuestCount,
    int OpenOrderCount,
    int UnclaimedOrderCount,
    int MediaCount,
    int OpenMusicRequestCount,
    int GuestbookEntryCount,
    string? TopDrinkName,
    int TopDrinkCount);

public sealed record PartyFeedItemResponse(
    string Type,
    string Emoji,
    string Text,
    DateTimeOffset CreatedAt);

public sealed record PartyMediaResponse(
    Guid Id,
    Guid GuestId,
    string GuestName,
    string MediaType,
    string FileName,
    string MimeType,
    long Size,
    string? Caption,
    DateTimeOffset CreatedAt,
    string ContentUrl);

public sealed record CreatePartyMusicRequest(string? Song, string? Artist, string? Comment);
public sealed record ChangePartyMusicStatusRequest(PartyMusicRequestStatus Status);
public sealed record PartyMusicResponse(
    Guid Id,
    Guid GuestId,
    string GuestName,
    string Song,
    string? Artist,
    string? Comment,
    PartyMusicRequestStatus Status,
    DateTimeOffset CreatedAt);

public sealed record CreatePartyGuestbookEntryRequest(string? Message);
public sealed record PartyGuestbookEntryResponse(
    Guid Id,
    Guid GuestId,
    string GuestName,
    string Message,
    DateTimeOffset CreatedAt);
