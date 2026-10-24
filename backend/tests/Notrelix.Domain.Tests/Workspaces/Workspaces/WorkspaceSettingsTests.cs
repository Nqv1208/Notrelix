using FluentAssertions;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Domain.Tests.Workspaces;

public class WorkspaceSettingsTests
{
    [Fact]
    public void Default_ShouldHaveBothFlagsFalse()
    {
        var settings = WorkspaceSettings.Create();

        settings.AllowPublicSharing.Should().BeFalse();
        settings.EnforceMfa.Should().BeFalse();
    }

    [Fact]
    public void Create_WithExplicitValues_ShouldSetProperties()
    {
        var settings = WorkspaceSettings.Create(allowPublicSharing: true, enforceMfa: true);

        settings.AllowPublicSharing.Should().BeTrue();
        settings.EnforceMfa.Should().BeTrue();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var s1 = WorkspaceSettings.Create(true, false);
        var s2 = WorkspaceSettings.Create(true, false);

        s1.Should().Be(s2);
        (s1 == s2).Should().BeTrue();
        s1.GetHashCode().Should().Be(s2.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        var s1 = WorkspaceSettings.Create(true, false);
        var s2 = WorkspaceSettings.Create(false, true);

        s1.Should().NotBe(s2);
        (s1 != s2).Should().BeTrue();
    }

    [Theory]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(180)]
    public void Create_WithAllowedGuestDefaultExpiry_ShouldKeepIt(int days)
    {
        WorkspaceSettings.Create(guestDefaultExpiryDays: days).GuestDefaultExpiryDays.Should().Be(days);
    }

    [Fact]
    public void Create_WithoutGuestDefaultExpiry_ShouldHaveNone()
    {
        WorkspaceSettings.Create().GuestDefaultExpiryDays.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(31)]
    [InlineData(365)]
    [InlineData(-30)]
    public void Create_WithOtherGuestDefaultExpiry_ShouldBeRejected(int days)
    {
        var act = () => WorkspaceSettings.Create(guestDefaultExpiryDays: days);

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Settings_GuestDefaultExpiryDaysInvalid);
    }

    [Fact]
    public void Equality_DifferentGuestDefaultExpiry_ShouldNotBeEqual()
    {
        WorkspaceSettings.Create(guestDefaultExpiryDays: 30).Should().NotBe(WorkspaceSettings.Create(guestDefaultExpiryDays: 90));
    }
}
