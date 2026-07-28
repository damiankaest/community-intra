using CommunityIntranet.BuildingBlocks.Authorization;

namespace CommunityIntranet.Modules.Members.Contracts;

public sealed record MemberResponse(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    PermissionRole PermissionRole,
    string? VisibleTitle,
    Guid? DepartmentId,
    string? DepartmentName,
    string? StatusMessage,
    DateTimeOffset JoinedAt,
    bool IsActive);

public sealed record UpdateMemberRequest(
    PermissionRole PermissionRole,
    string? VisibleTitle,
    Guid? DepartmentId,
    string? StatusMessage,
    bool IsActive);

public sealed record DepartmentResponse(
    Guid Id,
    string Name,
    string? Description,
    int SortOrder,
    string Icon,
    bool IsArchived);

public sealed record CreateDepartmentRequest(
    string Name,
    string? Description,
    string Icon);

public sealed record UpdateDepartmentRequest(
    string Name,
    string? Description,
    string Icon,
    int SortOrder);

public sealed record CreateInvitationRequest(
    PermissionRole DefaultPermissionRole,
    int ExpiresInDays = 7,
    int MaximumUses = 1);

public sealed record CreatedInvitationResponse(
    Guid Id,
    string Token,
    PermissionRole DefaultPermissionRole,
    DateTimeOffset ExpiresAt,
    int MaximumUses);

public sealed record InvitationResponse(
    Guid Id,
    string CreatedByDisplayName,
    PermissionRole DefaultPermissionRole,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int MaximumUses,
    int CurrentUses,
    bool IsRevoked,
    bool IsUsable);

public sealed record ResolveInvitationRequest(string Token);

public sealed record InvitationPreviewResponse(
    Guid InvitationId,
    Guid OrganizationId,
    string OrganizationName,
    string ThemePackKey,
    PermissionRole DefaultPermissionRole,
    DateTimeOffset ExpiresAt,
    int RemainingUses);

public sealed record AcceptInvitationRequest(string Token);

public sealed record AcceptedInvitationResponse(
    Guid OrganizationId,
    string OrganizationName,
    Guid MembershipId,
    PermissionRole PermissionRole);
