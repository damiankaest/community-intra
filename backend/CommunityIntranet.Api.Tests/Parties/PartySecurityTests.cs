using System.Text;
using CommunityIntranet.Modules.Parties.Services;
using Xunit;

namespace CommunityIntranet.Api.Tests.Parties;

public sealed class PartySecurityTests
{
    [Fact]
    public void SlugContainsReadablePrefixYearAndRandomComponent()
    {
        var first = PartySlugGenerator.Create("Annas Geburtstag", 2026);
        var second = PartySlugGenerator.Create("Annas Geburtstag", 2026);

        Assert.StartsWith("annas-geburtstag-2026-", first);
        Assert.NotEqual(first, second);
        Assert.Equal(5, first.Split('-')[^1].Length);
    }

    [Fact]
    public void GuestTokensAreOpaqueUniqueAndStoredAsHashes()
    {
        var first = PartyTokenService.CreateToken();
        var second = PartyTokenService.CreateToken();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("+", first);
        Assert.DoesNotContain("/", first);
        Assert.Equal(64, PartyTokenService.Hash(first).Length);
        Assert.Equal(PartyTokenService.Hash(first), PartyTokenService.Hash(first));
        Assert.NotEqual(first, PartyTokenService.Hash(first));
    }

    [Theory]
    [InlineData("image/jpeg", 12L * 1024 * 1024, "image")]
    [InlineData("video/mp4", 100L * 1024 * 1024, "video")]
    public void MediaRulesApplyExplicitLimits(string contentType, long maximumSize, string mediaType)
    {
        var rule = PartyMediaValidator.GetRule(contentType);

        Assert.NotNull(rule);
        Assert.Equal(maximumSize, rule.MaximumSize);
        Assert.Equal(mediaType, rule.MediaType);
    }

    [Fact]
    public async Task MediaSignatureAcceptsRealJpegHeader()
    {
        await using var stream = new MemoryStream([0xff, 0xd8, 0xff, 0xe0, 0, 0, 0, 0]);

        Assert.True(await PartyMediaValidator.HasValidSignatureAsync(
            stream,
            "image/jpeg",
            CancellationToken.None));
    }

    [Fact]
    public async Task MediaSignatureRejectsExecutableDisguisedAsImage()
    {
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes("MZ executable"));

        Assert.False(await PartyMediaValidator.HasValidSignatureAsync(
            stream,
            "image/jpeg",
            CancellationToken.None));
    }
}
