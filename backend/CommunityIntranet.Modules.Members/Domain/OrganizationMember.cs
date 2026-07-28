using CommunityIntranet.BuildingBlocks.Authorization;

namespace CommunityIntranet.Modules.Members.Domain;

public sealed class OrganizationMember
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public PermissionRole PermissionRole { get; set; }

    public string? VisibleTitle { get; set; }

    public Guid? DepartmentId { get; set; }

    public string? StatusMessage { get; set; }

    public DateTimeOffset JoinedAt { get; set; }

    public bool IsActive { get; set; }
}
