using FluentValidation.Results;
using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.Identity.Auth.Commands.ChangePassword;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ChangePasswordCommandValidatorTests
{
    [Fact]
    public void ANewPasswordEqualToTheCurrentOne_IsRefused()
    {
        var result = Validate("same-password-1", "same-password-1");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.PropertyName == nameof(ChangePasswordCommand.NewPassword)
            && e.ErrorMessage == "New password must be different from the current password");
    }

    [Fact]
    public void ANewPasswordBelowTheMinimumLength_IsRefused()
    {
        var result = Validate("current-password", new string('x', PasswordPolicy.MinimumLength - 1));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void AMissingCurrentPassword_IsRefused()
    {
        var result = Validate("", "new-password-1");

        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
    }

    [Fact]
    public void ADifferentPasswordAtTheMinimumLength_IsAccepted()
    {
        Validate("current-password", new string('y', PasswordPolicy.MinimumLength)).IsValid.Should().BeTrue();
    }

    private static ValidationResult Validate(string currentPassword, string newPassword) =>
        new ChangePasswordCommandValidator().Validate(
            new ChangePasswordCommand { CurrentPassword = currentPassword, NewPassword = newPassword });
}
