using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Profiles;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class UserProfileTests
{
    [Fact]
    public void Create_ShouldSetUserId()
    {
        var userId = Guid.NewGuid();

        var profile = UserProfile.Create(userId);

        profile.UserId.Should().Be(userId);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        var act = () => UserProfile.Create(Guid.Empty);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Profile_ShouldExtendEntity()
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        profile.Should().BeAssignableTo<Entity>();
    }

    [Fact]
    public void Profile_ShouldHaveId()
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        profile.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void UpdateTimezone_ShouldUseSuppliedTimestamp()
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdateTimezone("Asia/Ho_Chi_Minh", updateTime);

        profile.Timezone.Should().Be("Asia/Ho_Chi_Minh");
        profile.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void UpdateLocale_ShouldUseSuppliedTimestamp()
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdateLocale("en-US", updateTime);

        profile.Locale.Should().Be("en-US");
        profile.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void UpdateTheme_ShouldUseSuppliedTimestamp()
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdateTheme("dark", updateTime);

        profile.Theme.Should().Be("dark");
        profile.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void UpdatePreferences_ShouldUseSuppliedTimestamp()
    {
        var profile = UserProfile.Create(Guid.NewGuid());
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdatePreferences("{\"notifications\": true}", updateTime);

        profile.Preferences.Should().Be("{\"notifications\": true}");
        profile.UpdatedAt.Should().Be(updateTime);
    }

    [Fact]
    public void UpdateTimezone_WithEmptyValue_ShouldDefaultToUtc()
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        profile.UpdateTimezone("", DateTimeOffset.UtcNow);

        profile.Timezone.Should().Be("UTC");
    }

    [Fact]
    public void Profile_ShouldNotHaveUserNavigation()
    {
        var profile = UserProfile.Create(Guid.NewGuid());

        var userProperty = profile.GetType().GetProperty("User");
        userProperty.Should().BeNull("circular navigation to User has been removed");
    }
}
