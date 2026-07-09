using Notrelix.Application.Features.Identity.OAuth.Commands.StartOAuthLogin;
using Notrelix.Domain.Identity.OAuth;

namespace Notrelix.Integration.Tests.Auth;

public class StartOAuthLoginCommandValidatorTests
{
    private readonly StartOAuthLoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenProviderIsInvalid_ShouldFail()
    {
        var command = new StartOAuthLoginCommand
        {
            Provider = (OAuthProvider)999,
            ReturnUrl = null
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Fact]
    public void Validate_WhenProviderIsValid_ShouldPass()
    {
        var command = new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = null
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenReturnUrlIsAbsolute_ShouldFail()
    {
        var command = new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = "https://evil.com/phish"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReturnUrl");
    }

    [Fact]
    public void Validate_WhenReturnUrlIsRelative_ShouldPass()
    {
        var command = new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = "/dashboard"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenReturnUrlContainsDoubleSlash_ShouldFail()
    {
        var command = new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = "//evil.com/phish"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ReturnUrl");
    }

    [Fact]
    public void Validate_WhenReturnUrlIsEmpty_ShouldPass()
    {
        var command = new StartOAuthLoginCommand
        {
            Provider = OAuthProvider.Google,
            ReturnUrl = string.Empty
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
