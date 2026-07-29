namespace CommunityIntranet.Modules.Awards.Contracts;

public sealed record GrantAwardRequest(
    string Name,
    string Description,
    Guid AwardedToMemberId,
    string Icon,
    string Category,
    bool IsPublic);

public sealed record AwardResponse(
    Guid Id,
    string Name,
    string Description,
    Guid AwardedToMemberId,
    Guid AwardedByMemberId,
    DateTimeOffset AwardedAt,
    string Icon,
    string Category,
    bool IsPublic);

public sealed record AwardTemplateResponse(
    string Name,
    string DescriptionTemplate);
