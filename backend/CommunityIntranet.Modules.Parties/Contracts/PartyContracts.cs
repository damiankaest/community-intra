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

public sealed record CreatePartyMusicRequest(
    string? Song,
    string? Artist,
    string? Comment,
    string? SpotifyTrackId = null);
public sealed record ChangePartyMusicStatusRequest(PartyMusicRequestStatus Status);
public sealed record PartyMusicResponse(
    Guid Id,
    Guid GuestId,
    string GuestName,
    string Song,
    string? Artist,
    string? Comment,
    PartyMusicRequestStatus Status,
    DateTimeOffset CreatedAt,
    string? SpotifyTrackId = null,
    string? SpotifyUri = null,
    string? SpotifyAlbumImageUrl = null,
    int? DurationMs = null,
    DateTimeOffset? SpotifyQueuedAt = null,
    int VoteCount = 0,
    bool HasVoted = false);

public sealed record PartyMusicVoteResponse(int VoteCount, bool HasVoted);

public sealed record PartySpotifyConnectResponse(string AuthorizeUrl);
public sealed record UpdatePartySpotifyRequest(bool AutoQueue);
public sealed record PartySpotifyAdminStatusResponse(
    bool IsConfigured,
    bool IsConnected,
    string? AccountName,
    bool AutoQueue,
    SpotifyNowPlayingResponse? NowPlaying);
public sealed record PartySpotifyPublicStatusResponse(
    bool IsConnected,
    bool AutoQueue,
    SpotifyNowPlayingResponse? NowPlaying);
public sealed record SpotifyTrackResponse(
    string Id,
    string Uri,
    string Name,
    string Artist,
    string? AlbumImageUrl,
    int DurationMs);
public sealed record SpotifyNowPlayingResponse(
    bool IsPlaying,
    string Id,
    string Uri,
    string Name,
    string Artist,
    string? AlbumImageUrl,
    int DurationMs,
    int ProgressMs);

public sealed record CreatePartyGuestbookEntryRequest(string? Message);
public sealed record PartyGuestbookEntryResponse(
    Guid Id,
    Guid GuestId,
    string GuestName,
    string Message,
    DateTimeOffset CreatedAt);
