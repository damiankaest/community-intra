using CommunityIntranet.BuildingBlocks.Authorization;
using Xunit;

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

    [Theory]
    [InlineData(PermissionRole.Owner, true)]
    [InlineData(PermissionRole.Administrator, true)]
    [InlineData(PermissionRole.Moderator, true)]
    [InlineData(PermissionRole.Member, true)]
    [InlineData(PermissionRole.Guest, false)]
    public void CanCreateContentReflectsTechnicalRole(
        PermissionRole role,
        bool expected)
    {
        Assert.Equal(expected, role.CanCreateContent());
    }

    [Theory]
    [InlineData(PermissionRole.Owner, true)]
    [InlineData(PermissionRole.Administrator, true)]
    [InlineData(PermissionRole.Moderator, true)]
    [InlineData(PermissionRole.Member, false)]
    [InlineData(PermissionRole.Guest, false)]
    public void CanManageContentReflectsTechnicalRole(
        PermissionRole role,
        bool expected)
    {
        Assert.Equal(expected, role.CanManageContent());
        Assert.Equal(expected, role.CanGrantAwards());
    }
}
