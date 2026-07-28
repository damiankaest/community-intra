using CommunityIntranet.Modules.ThemePacks.Configuration;
using CommunityIntranet.Modules.ThemePacks.Seeding;
using Xunit;

namespace CommunityIntranet.Api.Tests.ThemePacks;

public sealed class ThemePackConfigurationValidatorTests
{
    [Fact]
    public void Validate_AcceptsBothSystemThemes()
    {
        foreach (var themePack in ThemePackSeeds.All)
        {
            var result = ThemePackConfigurationValidator.Validate(themePack);

            Assert.True(
                result.IsValid,
                string.Join(Environment.NewLine, result.Errors));
        }
    }

    [Fact]
    public void Validate_RejectsUnsafeMarkupAndUnknownIcon()
    {
        var source = ThemePackSeeds.All[0];
        var invalid = source with
        {
            Description = "<script>alert('nope')</script>",
            Visuals = source.Visuals with { LogoIcon = "custom-svg" }
        };

        var result = ThemePackConfigurationValidator.Validate(invalid);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "HTML-like markup",
                StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "LogoIcon",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsInvalidColorsAndVersion()
    {
        var source = ThemePackSeeds.All[0];
        var invalid = source with
        {
            Version = "latest",
            Visuals = source.Visuals with { PrimaryColor = "amber" }
        };

        var result = ThemePackConfigurationValidator.Validate(invalid);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "Version has an invalid format",
                StringComparison.Ordinal));
        Assert.Contains(
            result.Errors,
            error => error.Contains(
                "PrimaryColor must use #RRGGBB",
                StringComparison.Ordinal));
    }
}
