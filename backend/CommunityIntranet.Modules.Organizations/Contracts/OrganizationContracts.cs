using CommunityIntranet.BuildingBlocks.Authorization;

namespace CommunityIntranet.Modules.Organizations.Contracts;

public sealed record CreateOrganizationRequest(
    string Name,
    string? Description,
    string Language,
    string TimeZone,
    string? VisibleTitle);

public sealed record UpdateOrganizationRequest(
    string Name,
    string? Description,
    string Language,
    string TimeZone);

public sealed record OrganizationResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ThemePackId,
    string Language,
    string TimeZone,
    Guid OwnerUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsArchived,
    PermissionRole PermissionRole,
    string? VisibleTitle);

public sealed record OrganizationSummaryResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string Language,
    PermissionRole PermissionRole,
    string? VisibleTitle);
