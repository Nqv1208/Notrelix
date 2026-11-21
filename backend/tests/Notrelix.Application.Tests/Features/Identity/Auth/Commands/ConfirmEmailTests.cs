using Notrelix.Application.Features.Identity.Verification.Commands.ConfirmEmail;
using Notrelix.Application.Common.Tokens;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Tests.Features.Identity.Auth.Commands;

public class ConfirmEmailTests : IdentityHandlerTestBase
{
    private ConfirmEmailCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        OneTimeTokenServiceMock.Object,
        DateTimeProviderMock.Object,
        JwtBlacklistMock.Object,
        EmailServiceMock.Object,
        NullLogger<ConfirmEmailCommandHandler>.Instance);

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

    [Fact]
    public async Task Handle_WhenLinkConfirmsAPendingChange_MakesItTheSignInEmailEndsEverySessionAndNotifiesTheOldAddress()
    {
        var user = CreateUser(emailConfirmed: true);
        var oldEmail = user.Email.Value;
        user.RequestEmailChange("new@example.com", user.Id, TestNow);
        var token = CreateEmailVerificationToken(normalizedEmail: "new@example.com");
        var session = CreateSession();
        SetupUsers(user);
        SetupSessions(session);
        SetupEmailVerificationTokens(token);
        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("change-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var result = await CreateSut().Handle(new ConfirmEmailCommand("change-token"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.NormalizedEmail.Should().Be("new@example.com");
        user.EmailConfirmed.Should().BeTrue();
        user.PendingEmail.Should().BeNull();
        session.Status.Should().Be(SessionStatus.Revoked);
        EmailServiceMock.Verify(e => e.SendAsync(oldEmail, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheLinkNamesAnAddressThatIsNotPending_ChangesNothing()
    {
        var user = CreateUser(emailConfirmed: true);
        user.RequestEmailChange("new@example.com", user.Id, TestNow);
        var token = CreateEmailVerificationToken(normalizedEmail: "other@example.com");
        SetupUsers(user);
        SetupEmailVerificationTokens(token);
        OneTimeTokenServiceMock.Setup(o => o.ParseAndHash("stale-token", TokenPurpose.EmailVerification))
            .Returns(new ParsedOneTimeToken(token.TokenHash.Value, token.HashVersion));

        var result = await CreateSut().Handle(new ConfirmEmailCommand("stale-token"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        user.NormalizedEmail.Should().NotBe("new@example.com");
    }
}
