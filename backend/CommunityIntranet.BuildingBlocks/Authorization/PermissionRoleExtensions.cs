namespace CommunityIntranet.BuildingBlocks.Authorization;

public static class PermissionRoleExtensions
{
    public static bool CanManageOrganization(this PermissionRole role) =>
        role is PermissionRole.Owner or PermissionRole.Administrator;

    public static bool CanManageMembers(this PermissionRole role) =>
        role is PermissionRole.Owner
            or PermissionRole.Administrator
            or PermissionRole.Moderator;

    public static bool CanCreateContent(this PermissionRole role) =>
        role is PermissionRole.Owner
            or PermissionRole.Administrator
            or PermissionRole.Moderator
            or PermissionRole.Member;

    public static bool CanManageContent(this PermissionRole role) =>
        role is PermissionRole.Owner
            or PermissionRole.Administrator
            or PermissionRole.Moderator;

    public static bool CanGrantAwards(this PermissionRole role) =>
        role is PermissionRole.Owner
            or PermissionRole.Administrator
            or PermissionRole.Moderator;
}
