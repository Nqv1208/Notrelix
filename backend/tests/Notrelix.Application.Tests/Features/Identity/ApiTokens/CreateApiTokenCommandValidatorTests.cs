using Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;

namespace Notrelix.Application.Tests.Features.Identity.ApiTokens;

/// <summary>The request check that runs before any step-up proof is spent: the expiry is required and capped at 365 days.</summary>
public class CreateApiTokenCommandValidatorTests : IdentityHandlerTestBase
{
    private CreateApiTokenCommandValidator Sut() => new(DateTimeProviderMock.Object);

    private static CreateApiTokenCommand Command(DateTimeOffset? expiresAt) =>
        new(Guid.NewGuid(), "Deploy token", expiresAt, "proof");

    [Fact]
    public void WithoutExpiry_NamesTheRequiredExpiry()
    {
        var result = Sut().Validate(Command(null));

        result.Errors.Should().ContainSingle(e => e.ErrorCode == "identity.api-tokens.expiration-required");
    }

    [Fact]
    public void BeyondTheMaximumLifetime_NamesTheLimit()
    {
        var result = Sut().Validate(Command(TestNow.AddDays(CreateApiTokenCommandHandler.MaxTokenLifetimeDays).AddSeconds(1)));

        var error = result.Errors.Should().ContainSingle(e => e.ErrorCode == "identity.api-tokens.lifetime-exceeded").Subject;
        error.ErrorMessage.Should().Contain("365 days");
    }

    [Fact]
    public void ExactlyAtTheMaximumLifetime_IsValid()
    {
        Sut().Validate(Command(TestNow.AddDays(CreateApiTokenCommandHandler.MaxTokenLifetimeDays))).IsValid.Should().BeTrue();
    }

    [Fact]
    public void InThePast_IsRefused()
    {
        var result = Sut().Validate(Command(TestNow.AddMinutes(-1)));

        result.Errors.Should().Contain(e => e.ErrorCode == "identity.api-tokens.invalid-expiration");
    }
}
