namespace CommunityIntranet.BuildingBlocks.Authorization;

public static class PermissionRoleExtensions
{
    public static bool CanManageOrganization(this PermissionRole role) =>
        role is PermissionRole.Owner or PermissionRole.Administrator;

    public static bool CanManageMembers(this PermissionRole role) =>
        role is PermissionRole.Owner
            or PermissionRole.Administrator
            or PermissionRole.Moderator;
}
