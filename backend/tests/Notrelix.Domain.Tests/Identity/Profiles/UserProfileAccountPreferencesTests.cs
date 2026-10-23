using Notrelix.Domain.Identity;
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

public class UserProfilePhoneTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 10, 23, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = CreatedAt.AddHours(1);

    private static UserProfile NewProfile() => UserProfile.Create(Guid.NewGuid(), CreatedAt);

    [Fact]
    public void NewProfile_HasNoPhone_AndKeepsItSelfOnly()
    {
        var profile = NewProfile();

        profile.Phone.Should().BeNull();
        profile.PhoneVisibleToWorkspaceMembers.Should().BeFalse();
    }

    [Theory]
    [InlineData("+84901234567", "+84901234567")]
    [InlineData("  +84 90-123.4567 ", "+84901234567")]
    [InlineData("+1 (415) 555-0132", "+14155550132")]
    [InlineData("+12345678", "+12345678")]
    [InlineData("+123456789012345", "+123456789012345")]
    public void Apply_NormalizesAnE164ishPhone(string input, string stored)
    {
        var profile = NewProfile();

        profile.ApplyAccountPreferences(null, null, null, Later, phone: input).Should().BeTrue();

        profile.Phone.Should().Be(stored);
    }

    [Theory]
    [InlineData("0901234567")]
    [InlineData("84901234567")]
    [InlineData("+0901234567")]
    [InlineData("+1234567")]
    [InlineData("+1234567890123456")]
    [InlineData("+84 90x123 4567")]
    [InlineData("++84901234567")]
    [InlineData("+84901234567 ext 12")]
    public void Apply_RejectsAnInvalidPhone_WithoutPartialChange(string input)
    {
        var profile = NewProfile();
        var version = profile.Version;

        var act = () => profile.ApplyAccountPreferences("en", null, null, Later, phone: input);

        act.Should().Throw<BusinessRuleException>().Which.RuleCode.Should().Be(IdentityRuleCodes.Identity_Profile_InvalidPhone);
        profile.Locale.Should().Be("vi", "a rejected phone leaves every other value unchanged");
        profile.Version.Should().Be(version);
    }

    [Fact]
    public void Apply_BlankClearsThePhone_AndOmittedKeepsIt()
    {
        var profile = NewProfile();
        profile.ApplyAccountPreferences(null, null, null, Later, phone: "+84901234567");

        profile.ApplyAccountPreferences(null, null, "dark", Later.AddMinutes(1));
        profile.Phone.Should().Be("+84901234567");

        profile.ApplyAccountPreferences(null, null, null, Later.AddMinutes(2), phone: "  ").Should().BeTrue();
        profile.Phone.Should().BeNull();
    }

    [Fact]
    public void Apply_SamePhoneAndFlag_IsASemanticNoOp()
    {
        var profile = NewProfile();
        profile.ApplyAccountPreferences(null, null, null, Later, phone: "+84901234567", phoneVisibleToWorkspaceMembers: true);
        var version = profile.Version;

        var changed = profile.ApplyAccountPreferences(null, null, null, Later.AddMinutes(1), phone: "+84 901 234 567", phoneVisibleToWorkspaceMembers: true);

        changed.Should().BeFalse();
        profile.Version.Should().Be(version);
    }

    [Fact]
    public void Apply_FlagAlone_TogglesVisibility_AsOneVersionStep()
    {
        var profile = NewProfile();
        var version = profile.Version;

        profile.ApplyAccountPreferences(null, null, null, Later, phoneVisibleToWorkspaceMembers: true).Should().BeTrue();

        profile.PhoneVisibleToWorkspaceMembers.Should().BeTrue();
        profile.Version.Should().Be(version + 1);
    }
}
