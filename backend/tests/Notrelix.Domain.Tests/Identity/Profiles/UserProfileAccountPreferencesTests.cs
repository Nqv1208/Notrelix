using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserProfileAccountPreferencesTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 6, 11, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = CreatedAt.AddHours(1);

    private static UserProfile NewProfile() => UserProfile.Create(Guid.NewGuid(), CreatedAt);

    [Fact]
    public void Apply_ChangesSuppliedValuesAsOneVersionStep()
    {
        var profile = NewProfile();
        var version = profile.Version;

        var changed = profile.ApplyAccountPreferences("en-US", "Asia/Tokyo", "Dark", Later);

        changed.Should().BeTrue();
        profile.Locale.Should().Be("en-US");
        profile.Timezone.Should().Be("Asia/Tokyo");
        profile.Theme.Should().Be("dark");
        profile.Version.Should().Be(version + 1);
        profile.UpdatedAt.Should().Be(Later);
    }

    [Fact]
    public void Apply_LeavesOmittedValuesAlone()
    {
        var profile = NewProfile();

        profile.ApplyAccountPreferences(null, null, "light", Later);

        profile.Theme.Should().Be("light");
        profile.Locale.Should().Be("vi");
        profile.Timezone.Should().Be("UTC");
    }

    [Fact]
    public void Apply_WhenNothingChanges_IsASemanticNoOp()
    {
        var profile = NewProfile();
        var version = profile.Version;

        var changed = profile.ApplyAccountPreferences("vi", "UTC", "system", Later);

        changed.Should().BeFalse();
        profile.Version.Should().Be(version);
        profile.UpdatedAt.Should().NotBe(Later);
    }

    [Theory]
    [InlineData("x", null, null)]
    [InlineData("english", null, null)]
    [InlineData("en_US", null, null)]
    [InlineData(null, "", null)]
    [InlineData(null, "Asia/Ho Chi Minh", null)]
    [InlineData(null, "../etc", null)]
    [InlineData(null, "A/B/C/D", null)]
    [InlineData(null, null, "neon")]
    [InlineData(null, null, "")]
    public void Apply_WithAnInvalidValue_ThrowsAndChangesNothing(string? locale, string? timezone, string? theme)
    {
        var profile = NewProfile();
        var version = profile.Version;

        var act = () => profile.ApplyAccountPreferences(locale, timezone, theme, Later);

        act.Should().Throw<BusinessRuleException>();
        profile.Version.Should().Be(version);
        profile.Locale.Should().Be("vi");
        profile.Timezone.Should().Be("UTC");
        profile.Theme.Should().Be("system");
    }

    [Fact]
    public void Apply_WithOneInvalidValueAmongValidOnes_AppliesNone()
    {
        var profile = NewProfile();

        var act = () => profile.ApplyAccountPreferences("en", "Asia/Tokyo", "neon", Later);

        act.Should().Throw<BusinessRuleException>();
        profile.Locale.Should().Be("vi");
        profile.Timezone.Should().Be("UTC");
    }

    [Fact]
    public void Apply_OnADeletedProfile_Throws()
    {
        var profile = NewProfile();
        profile.Delete(profile.UserId, Later);

        var act = () => profile.ApplyAccountPreferences("en", null, null, Later.AddMinutes(1));

        act.Should().Throw<BusinessRuleException>();
    }
}
