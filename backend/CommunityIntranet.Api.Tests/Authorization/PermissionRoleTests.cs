using CommunityIntranet.BuildingBlocks.Authorization;

namespace CommunityIntranet.Api.Tests.Authorization;

public sealed class PermissionRoleTests
{
    [Theory]
    [InlineData(PermissionRole.Owner, true)]
    [InlineData(PermissionRole.Administrator, true)]
    [InlineData(PermissionRole.Moderator, false)]
    [InlineData(PermissionRole.Member, false)]
    [InlineData(PermissionRole.Guest, false)]
    public void CanManageOrganizationReflectsTechnicalRole(
        PermissionRole role,
        bool expected)
    {
        Assert.Equal(expected, role.CanManageOrganization());
    }
}
