using CommunityIntranet.Modules.Members.Services;

namespace CommunityIntranet.Api.Tests.Members;

public sealed class InvitationTokenServiceTests
{
    [Fact]
    public void Create_ReturnsUniqueOpaqueTokensAndHashes()
    {
        var service = new InvitationTokenService();

        var first = service.Create();
        var second = service.Create();

        Assert.NotEqual(first.RawToken, second.RawToken);
        Assert.NotEqual(first.TokenHash, second.TokenHash);
        Assert.DoesNotContain("+", first.RawToken);
        Assert.DoesNotContain("/", first.RawToken);
        Assert.Equal(64, first.TokenHash.Length);
        Assert.Equal(
            first.TokenHash,
            InvitationTokenService.Hash(first.RawToken));
    }

    [Fact]
    public void Hash_ReturnsSameValueForSameToken()
    {
        const string rawToken = "safe-token-value";

        var first = InvitationTokenService.Hash(rawToken);
        var second = InvitationTokenService.Hash(rawToken);

        Assert.Equal(first, second);
    }
}
