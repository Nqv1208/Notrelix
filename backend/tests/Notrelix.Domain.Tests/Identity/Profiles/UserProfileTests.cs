using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity;

[CoversAggregate(typeof(UserProfile))]
public class UserProfileTests
{
    private static readonly DateTimeOffset SampleCreatedAt = new DateTimeOffset(2026, 6, 11, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldSetUserIdAndAudit()
    {
        var userId = Guid.NewGuid();

        var profile = UserProfile.Create(userId, SampleCreatedAt);

        profile.UserId.Should().Be(userId);
        profile.CreatedBy.Should().Be(userId);
        profile.CreatedAt.Should().Be(SampleCreatedAt);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        var act = () => UserProfile.Create(Guid.Empty, SampleCreatedAt);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Profile_ShouldExtendAggregateRoot()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), SampleCreatedAt);

        profile.Should().BeAssignableTo<AggregateRoot>();
    }

    [Fact]
    public void Profile_ShouldHaveId()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), SampleCreatedAt);

        profile.Id.Should().NotBe(Guid.Empty);
    }

    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdatePreferences), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTheme), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateLocale), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTimezone), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateTimezone_ShouldUseSuppliedTimestampAndAudit()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, SampleCreatedAt);
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdateTimezone("Asia/Ho_Chi_Minh", updateTime);

        profile.Timezone.Should().Be("Asia/Ho_Chi_Minh");
        profile.UpdatedAt.Should().Be(updateTime);
        profile.UpdatedBy.Should().Be(userId);
    }

    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdatePreferences), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTheme), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateLocale), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTimezone), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateLocale_ShouldUseSuppliedTimestampAndAudit()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, SampleCreatedAt);
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdateLocale("en-US", updateTime);

        profile.Locale.Should().Be("en-US");
        profile.UpdatedAt.Should().Be(updateTime);
        profile.UpdatedBy.Should().Be(userId);
    }

    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdatePreferences), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTheme), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateLocale), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTimezone), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateTheme_ShouldUseSuppliedTimestampAndAudit()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, SampleCreatedAt);
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdateTheme("dark", updateTime);

        profile.Theme.Should().Be("dark");
        profile.UpdatedAt.Should().Be(updateTime);
        profile.UpdatedBy.Should().Be(userId);
    }

    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdatePreferences), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTheme), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateLocale), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTimezone), MutationScenario.Audit, typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void UpdatePreferences_ShouldUseSuppliedTimestampAndAudit()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfile.Create(userId, SampleCreatedAt);
        var updateTime = new DateTimeOffset(2026, 6, 11, 10, 0, 0, TimeSpan.Zero);

        profile.UpdatePreferences("{\"notifications\": true}", updateTime);

        profile.Preferences.Should().Be("{\"notifications\": true}");
        profile.UpdatedAt.Should().Be(updateTime);
        profile.UpdatedBy.Should().Be(userId);
    }

    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdatePreferences), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTheme), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateLocale), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTimezone), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateTimezone_WithEmptyValue_ShouldDefaultToUtc()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), SampleCreatedAt);

        profile.UpdateTimezone("", DateTimeOffset.UtcNow);

        profile.Timezone.Should().Be("UTC");
    }

    [Fact]
    public void Profile_ShouldNotHaveUserNavigation()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), SampleCreatedAt);

        var userProperty = profile.GetType().GetProperty("User");
        userProperty.Should().BeNull("circular navigation to User has been removed");
    }

    [CoversMutation(typeof(UserProfile), nameof(UserProfile.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdatePreferences), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTheme), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateLocale), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [CoversMutation(typeof(UserProfile), nameof(UserProfile.UpdateTimezone), MutationScenario.Invalid, typeof(string), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateTimezone_OnDeletedProfile_ShouldThrow()
    {
        var profile = UserProfile.Create(Guid.NewGuid(), SampleCreatedAt);
        profile.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => profile.UpdateTimezone("UTC", DateTimeOffset.UtcNow);

        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }
}
