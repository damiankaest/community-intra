using CommunityIntranet.Modules.Identity.Services;
using Xunit;

namespace CommunityIntranet.Api.Tests.Identity;

public sealed class PasswordResetTokenCodecTests
{
    [Fact]
    public void ResetTokenRoundTripsWithoutQueryStringUnsafeCharacters()
    {
        const string token = "identity+/reset==token with spaces";

        var encoded = PasswordResetTokenCodec.Encode(token);

        Assert.DoesNotContain("+", encoded);
        Assert.DoesNotContain("/", encoded);
        Assert.DoesNotContain("=", encoded);
        Assert.True(PasswordResetTokenCodec.TryDecode(encoded, out var decoded));
        Assert.Equal(token, decoded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("%%%")]
    public void InvalidResetTokenIsRejected(string token)
    {
        Assert.False(PasswordResetTokenCodec.TryDecode(token, out _));
    }
}
