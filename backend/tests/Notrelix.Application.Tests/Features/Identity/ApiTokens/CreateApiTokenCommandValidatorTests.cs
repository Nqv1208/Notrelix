using Notrelix.Application.Features.Identity.ApiTokens.Commands.CreateApiToken;

namespace Notrelix.Application.Tests.Features.Identity.ApiTokens;

/// <summary>The request check that runs before any step-up proof is spent: the expiry is required and capped at 365 days.</summary>
public class CreateApiTokenCommandValidatorTests : IdentityHandlerTestBase
{
    private CreateApiTokenCommandValidator Sut() => new(DateTimeProviderMock.Object);

    private static CreateApiTokenCommand Command(DateTimeOffset? expiresAt, params string[] scopes) =>
        new(Guid.NewGuid(), "Deploy token", expiresAt, scopes.Length == 0 ? ["work:read"] : scopes, "proof");

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

    [Fact]
    public void WithNoScope_NamesTheRequiredScope()
    {
        var command = new CreateApiTokenCommand(Guid.NewGuid(), "Deploy token", TestNow.AddDays(30), [], "proof");

        Sut().Validate(command).Errors.Should().ContainSingle(e => e.ErrorCode == "identity.api-tokens.scopes-required");
    }

    [Fact]
    public void WithAScopeOutsideTheVocabulary_NamesIt()
    {
        var result = Sut().Validate(Command(TestNow.AddDays(30), "work:read", "admin"));

        var error = result.Errors.Should().ContainSingle(e => e.ErrorCode == "identity.api-tokens.unknown-scope").Subject;
        error.ErrorMessage.Should().Contain("admin");
    }

    [Fact]
    public void WithEveryScopeOfTheVocabulary_IsValid()
    {
        Sut().Validate(Command(TestNow.AddDays(30), [.. Notrelix.Domain.Identity.Tokens.ApiTokenScopes.Vocabulary])).IsValid.Should().BeTrue();
    }
}
