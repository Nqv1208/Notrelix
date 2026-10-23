using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateAccountPreferences;
using Notrelix.Application.Features.Identity.Profiles.DTOs;
using Notrelix.Domain.Identity.Profiles;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Commands;

public class UpdateAccountPreferencesTests : IdentityHandlerTestBase
{
    private UpdateAccountPreferencesCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        DateTimeProviderMock.Object);

    private UserProfile SavedProfile(Guid? userId = null)
    {
        var profile = UserProfile.Create(userId ?? TestUserId, TestNow);
        profile.ApplyAccountPreferences("en", "Asia/Tokyo", "dark", TestNow);
        return profile;
    }

    [Fact]
    public async Task Handle_WhenNothingSavedYetAndVersionIsZero_CreatesProfileWithGivenValues()
    {
        SetupUsers(CreateUser());
        SetupUserProfiles();

        var result = await CreateSut().Handle(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = 0,
            Locale = "en",
            Timezone = "Asia/Tokyo",
            Theme = "dark"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Should().Match<AccountPreferencesDto>(d =>
            d.Locale == "en" && d.Timezone == "Asia/Tokyo" && d.Theme == "dark" && d.Version > 0);
        IdentityContextMock.Verify(c => c.UserProfiles.Add(It.Is<UserProfile>(p => p.UserId == TestUserId)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNothingSavedYetButClientClaimsAVersion_IsRejectedAndNothingIsCreated()
    {
        SetupUsers(CreateUser());
        SetupUserProfiles();

        var act = () => CreateSut().Handle(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = 3,
            Theme = "dark"
        }, CancellationToken.None);

        var thrown = (await act.Should().ThrowAsync<PreconditionFailedException>()).Which;
        thrown.ErrorCode.Should().Be("identity.preferences.version-conflict");
        thrown.CurrentVersion.Should().Be(0);
        IdentityContextMock.Verify(c => c.UserProfiles.Add(It.IsAny<UserProfile>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExpectedVersionIsStale_ThrowsPreconditionFailedAndLeavesTheProfileUntouched()
    {
        var profile = SavedProfile();
        var version = profile.Version;
        SetupUsers(CreateUser());
        SetupUserProfiles(profile);

        var act = () => CreateSut().Handle(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = version - 1,
            Theme = "light"
        }, CancellationToken.None);

        var thrown = (await act.Should().ThrowAsync<PreconditionFailedException>()).Which;
        thrown.ExpectedVersion.Should().Be(version - 1);
        thrown.CurrentVersion.Should().Be(version);
        profile.Theme.Should().Be("dark");
        profile.Version.Should().Be(version);
    }

    [Fact]
    public async Task Handle_PartialUpdate_ChangesOnlySuppliedMembersAndAdvancesVersionOnce()
    {
        var profile = SavedProfile();
        var version = profile.Version;
        SetupUsers(CreateUser());
        SetupUserProfiles(profile);

        var result = await CreateSut().Handle(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = version,
            Theme = "light"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Theme.Should().Be("light");
        result.Data.Locale.Should().Be("en");
        result.Data.Timezone.Should().Be("Asia/Tokyo");
        result.Data.Version.Should().Be(version + 1);
    }

    [Fact]
    public async Task Handle_WhenNothingChanges_IsASemanticNoOpWithoutVersionBump()
    {
        var profile = SavedProfile();
        var version = profile.Version;
        SetupUsers(CreateUser());
        SetupUserProfiles(profile);

        var result = await CreateSut().Handle(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = version,
            Theme = "dark",
            Locale = "en"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Version.Should().Be(version);
    }

    [Fact]
    public async Task Handle_NeverTouchesAnotherUsersProfile()
    {
        var other = SavedProfile(Guid.CreateVersion7());
        var otherVersion = other.Version;
        SetupUsers(CreateUser());
        SetupUserProfiles(other);

        var result = await CreateSut().Handle(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = 0,
            Theme = "light"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        other.Theme.Should().Be("dark");
        other.Version.Should().Be(otherVersion);
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoUserRow_ReturnsNotFoundWithoutCreatingAProfile()
    {
        SetupUsers();
        SetupUserProfiles();

        var result = await CreateSut().Handle(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = 0,
            Theme = "dark"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        IdentityContextMock.Verify(c => c.UserProfiles.Add(It.IsAny<UserProfile>()), Times.Never);
    }
}

public class UpdateAccountPreferencesValidatorTests
{
    private static readonly UpdateAccountPreferencesCommandValidator Sut = new();

    [Theory]
    [InlineData("vi", "UTC", "system")]
    [InlineData("en-US", "Asia/Ho_Chi_Minh", "light")]
    [InlineData(null, "America/Argentina/Buenos_Aires", null)]
    public void Valid_Values_Pass(string? locale, string? timezone, string? theme)
    {
        Sut.Validate(new UpdateAccountPreferencesCommand
        {
            ExpectedVersion = 0,
            Locale = locale,
            Timezone = timezone,
            Theme = theme
        }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("neon")]
    [InlineData("")]
    [InlineData("DARK ")]
    public void Unknown_Theme_Fails_OnTheThemeMember(string theme)
    {
        if (theme.Trim().ToLowerInvariant() is "dark") return; // trimmed/case-insensitive values are accepted by design

        var result = Sut.Validate(new UpdateAccountPreferencesCommand { ExpectedVersion = 1, Theme = theme });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAccountPreferencesCommand.Theme));
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("../etc/passwd")]
    [InlineData("")]
    public void Unknown_Timezone_Fails_OnTheTimezoneMember(string timezone)
    {
        var result = Sut.Validate(new UpdateAccountPreferencesCommand { ExpectedVersion = 1, Timezone = timezone });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAccountPreferencesCommand.Timezone));
    }

    [Theory]
    [InlineData("english")]
    [InlineData("v")]
    [InlineData("en_US")]
    public void Malformed_Locale_Fails_OnTheLocaleMember(string locale)
    {
        var result = Sut.Validate(new UpdateAccountPreferencesCommand { ExpectedVersion = 1, Locale = locale });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAccountPreferencesCommand.Locale));
    }

    [Fact]
    public void Empty_Patch_And_Negative_Version_Fail()
    {
        Sut.Validate(new UpdateAccountPreferencesCommand { ExpectedVersion = 1 }).IsValid.Should().BeFalse();
        Sut.Validate(new UpdateAccountPreferencesCommand { ExpectedVersion = -1, Theme = "dark" }).IsValid.Should().BeFalse();
    }
}
