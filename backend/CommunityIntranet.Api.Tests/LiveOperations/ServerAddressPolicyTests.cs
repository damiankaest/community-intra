using System.Net;
using CommunityIntranet.Modules.LiveOperations.Services;

namespace CommunityIntranet.Api.Tests.LiveOperations;

public sealed class ServerAddressPolicyTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.16.1.2")]
    [InlineData("192.168.1.2")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.1.2")]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fd00::1")]
    public void IsPublicAddressRejectsInternalTargets(string value)
    {
        Assert.False(ServerAddressPolicy.IsPublicAddress(
            IPAddress.Parse(value)));
    }

    [Theory]
    [InlineData("1.1.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("2606:4700:4700::1111")]
    public void IsPublicAddressAcceptsPublicTargets(string value)
    {
        Assert.True(ServerAddressPolicy.IsPublicAddress(
            IPAddress.Parse(value)));
    }

    [Theory]
    [InlineData("games.example.com")]
    [InlineData("93.184.216.34")]
    [InlineData("2606:4700:4700::1111")]
    public void IsValidHostAcceptsHostWithoutSchemeOrPath(string value)
    {
        Assert.True(ServerAddressPolicy.IsValidHost(value));
    }

    [Theory]
    [InlineData("https://games.example.com")]
    [InlineData("games.example.com/api/v1")]
    [InlineData("user@games.example.com")]
    public void IsValidHostRejectsUrlParts(string value)
    {
        Assert.False(ServerAddressPolicy.IsValidHost(value));
    }
}
