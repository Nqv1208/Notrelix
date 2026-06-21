using FluentAssertions;
using Notrelix.Domain.Identity.Profiles;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class UserProfileThemeTests
{
    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    [InlineData("system")]
    [InlineData("LIGHT")]
    [InlineData("Dark")]
    [InlineData("  light  ")]
    public void IsValid_ValidTheme_ShouldReturnTrue(string theme)
    {
        var result = UserProfileTheme.IsValid(theme);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("luna")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("blue")]
    public void IsValid_InvalidTheme_ShouldReturnFalse(string theme)
    {
        var result = UserProfileTheme.IsValid(theme);

        result.Should().BeFalse();
    }
}
