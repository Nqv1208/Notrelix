using FluentAssertions;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Profiles;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class UserPreferencesTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var prefs = UserPreferences.Create(JsonValue.Create("{\"notifications\":true}"));

        prefs.Data.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithNull_ShouldThrow()
    {
        var act = () => UserPreferences.Create(null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Default_ShouldReturnEmptyObject()
    {
        var prefs = UserPreferences.Default();

        prefs.Data.Should().NotBeNull();
    }

    [Fact]
    public void Equality_SameData_ShouldBeEqual()
    {
        var data = JsonValue.Create("{\"a\":1}");
        var prefs1 = UserPreferences.Create(data);
        var prefs2 = UserPreferences.Create(data);

        prefs1.Should().Be(prefs2);
        (prefs1 == prefs2).Should().BeTrue();
        prefs1.GetHashCode().Should().Be(prefs2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentData_ShouldNotBeEqual()
    {
        var prefs1 = UserPreferences.Create(JsonValue.Create("{\"a\":1}"));
        var prefs2 = UserPreferences.Create(JsonValue.Create("{\"a\":2}"));

        prefs1.Should().NotBe(prefs2);
        (prefs1 != prefs2).Should().BeTrue();
    }
}
