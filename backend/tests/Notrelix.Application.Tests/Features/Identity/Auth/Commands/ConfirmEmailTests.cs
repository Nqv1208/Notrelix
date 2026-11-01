using Notrelix.Application.Features.Identity.Verification.Commands.ConfirmEmail;
using Notrelix.Application.Common.Tokens;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ConfirmEmailTests : IdentityHandlerTestBase
{
    private ConfirmEmailCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        OneTimeTokenServiceMock.Object,
        DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenValidToken_ConfirmsEmail()
    {
        var user = CreateUser(emailConfirmed: false);
        var token = CreateEmailVerificationToken(normalizedEmail: user.NormalizedEmail);
        SetupUsers(user);
        SetupEmailVerificationTokens(token);

        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("valid-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var sut = CreateSut();
        var result = await sut.Handle(new ConfirmEmailCommand("valid-token"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.EmailConfirmed.Should().BeTrue();
        result.Data!.SessionRefreshRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenActiveTokenIsPastExpiry_AnswersTheExpiredCode()
    {
        var user = CreateUser(emailConfirmed: false);
        var token = CreateEmailVerificationToken(normalizedEmail: user.NormalizedEmail);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(token.ExpiresAt.AddMinutes(1));
        SetupUsers(user);
        SetupEmailVerificationTokens(token);

        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("expired-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var sut = CreateSut();
        var result = await sut.Handle(new ConfirmEmailCommand("expired-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle()
            .Which.Code.Should().Be(ConfirmEmailCommandHandler.TokenExpiredCode);
        result.Errors.Should().ContainSingle().Which.Should().Be("Verification token has expired.");
    }

    [Fact]
    public async Task Handle_WhenTokenWasAlreadyUsed_AnswersTheGenericRefusal()
    {
        var user = CreateUser(emailConfirmed: false);
        var token = CreateEmailVerificationToken(normalizedEmail: user.NormalizedEmail);
        token.MarkUsed(TestNow);
        SetupUsers(user);
        SetupEmailVerificationTokens(token);

        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("used-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var sut = CreateSut();
        var result = await sut.Handle(new ConfirmEmailCommand("used-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().BeEmpty("a used link must not read as an expired one");
        result.Errors.Should().ContainSingle().Which.Should().Be("Invalid or expired verification token.");
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsFailure()
    {
        var user = CreateUser(emailConfirmed: false);
        SetupUsers(user);
        SetupEmailVerificationTokens();

        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("bad-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken("nonexistent-hash", 1));

        var sut = CreateSut();
        var result = await sut.Handle(new ConfirmEmailCommand("bad-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().BeEmpty();
        result.Errors.Should().ContainSingle().Which.Should().Be("Invalid or expired verification token.");
    }
}
