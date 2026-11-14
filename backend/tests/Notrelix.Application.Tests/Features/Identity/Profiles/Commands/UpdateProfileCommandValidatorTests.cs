using FluentValidation.Results;
using Notrelix.Application.Features.Identity.Profiles.Commands.UpdateProfile;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Profiles.Commands;

public class UpdateProfileCommandValidatorTests
{
    [Fact]
    public void ANameAtTheLimit_IsAccepted()
    {
        Validate(new string('n', User.MaxNameLength)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ANameOneCharacterOverTheLimit_IsRefused()
    {
        var result = Validate(new string('n', User.MaxNameLength + 1));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ABlankName_IsRefused(string name)
    {
        Validate(name).Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.Name));
    }

    [Theory]
    [InlineData("https://example.com/avatar.png")]
    [InlineData("https://cdn.example.com/a/b.webp")]
    public void AnAbsoluteHttpsPhotoAddress_IsAccepted(string avatar)
    {
        Validate("Ann", avatar).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://example.com/avatar.png")]
    [InlineData("https://user:secret@example.com/avatar.png")]
    [InlineData("/avatar.png")]
    [InlineData("javascript:alert(1)")]
    public void APhotoAddressThatIsNotAnAbsoluteHttpsAddressWithoutCredentials_IsRefused(string avatar)
    {
        Validate("Ann", avatar).Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.Avatar));
    }

    [Fact]
    public void APhotoAddressOver500Characters_IsRefused()
    {
        var avatar = "https://example.com/" + new string('a', 500);

        Validate("Ann", avatar).Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileCommand.Avatar));
    }

    private static ValidationResult Validate(string name, string? avatar = null) =>
        new UpdateProfileCommandValidator().Validate(new UpdateProfileCommand { Name = name, Avatar = avatar });
}
