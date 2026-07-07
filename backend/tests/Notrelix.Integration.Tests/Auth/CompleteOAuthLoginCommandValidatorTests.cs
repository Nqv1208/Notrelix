using Notrelix.Application.Features.Identity.OAuth.Commands.CompleteOAuthLogin;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Integration.Tests.Auth;

public class CompleteOAuthLoginCommandValidatorTests
{
    private readonly CompleteOAuthLoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenProviderIsInvalid_ShouldFail()
    {
        var command = new CompleteOAuthLoginCommand
        {
            Provider = (OAuthProvider)999,
            Code = "code",
            State = "state"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Fact]
    public void Validate_WhenCodeIsMissingAndNoError_ShouldFail()
    {
        var command = new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = string.Empty,
            State = "state"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }

    [Fact]
    public void Validate_WhenStateIsMissingAndNoError_ShouldFail()
    {
        var command = new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "code",
            State = string.Empty
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "State");
    }

    [Fact]
    public void Validate_WhenValid_ShouldPass()
    {
        var command = new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = "auth-code",
            State = "state-value"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenErrorPresent_ShouldNotRequireCodeOrState()
    {
        var command = new CompleteOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            Code = string.Empty,
            State = string.Empty,
            Error = "access_denied"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
