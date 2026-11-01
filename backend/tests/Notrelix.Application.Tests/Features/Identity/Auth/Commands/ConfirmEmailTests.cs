using Notrelix.Application.Features.Identity.Verification.Commands.ConfirmEmail;
using Notrelix.Application.Common.Tokens;
using Notrelix.Domain.Identity.Tokens;

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
    public async Task Handle_WhenTokenExpired_ReturnsFailure()
    {
        var user = CreateUser(emailConfirmed: false);
        var token = CreateEmailVerificationToken(normalizedEmail: user.NormalizedEmail);
        token.Expire(TestNow.AddDays(2));
        SetupUsers(user);
        SetupEmailVerificationTokens(token);

        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("expired-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var sut = CreateSut();
        var result = await sut.Handle(new ConfirmEmailCommand("expired-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("expired"));
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
        result.Errors.Should().Contain(e => e.Contains("Invalid or expired"));
    }
    [Fact]
    public async Task Handle_WhenAnActiveTokenIsPastItsExpiry_FailsConfirmsNothingAndExpiresIt()
    {
        var user = CreateUser(emailConfirmed: false);
        var token = CreateEmailVerificationToken(normalizedEmail: user.NormalizedEmail);
        SetupUsers(user);
        SetupEmailVerificationTokens(token);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow.AddDays(2));
        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("late-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var result = await CreateSut().Handle(new ConfirmEmailCommand("late-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        user.EmailConfirmed.Should().BeFalse();
        token.Status.Should().Be(UserTokenStatus.Expired);
    }

    [Fact]
    public async Task Handle_WhenTheTokenWasAlreadyUsed_FailsAsAnUnknownOneDoesAndChangesNothing()
    {
        var user = CreateUser(emailConfirmed: false);
        var token = CreateEmailVerificationToken(normalizedEmail: user.NormalizedEmail);
        token.MarkUsed(TestNow);
        SetupUsers(user);
        SetupEmailVerificationTokens(token);
        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("used-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var result = await CreateSut().Handle(new ConfirmEmailCommand("used-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Invalid or expired"));
        user.EmailConfirmed.Should().BeFalse();
    }
}
