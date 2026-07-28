using CommunityIntranet.Modules.Organizations.Services;
using Xunit;

namespace CommunityIntranet.Api.Tests.Organizations;

public sealed class SlugGeneratorTests
{
    [Theory]
    [InlineData("Rheinische FICSIT-Niederlassung", "rheinische-ficsit-niederlassung")]
    [InlineData("  Förderband & Logistik  ", "forderband-logistik")]
    [InlineData("運送部門", "organization")]
    public void CreateReturnsUrlSafeSlug(string value, string expected)
    {
        Assert.Equal(expected, SlugGenerator.Create(value));
    }
}
