using Notrelix.Application.Common.Security.Auth;
using Notrelix.Application.Features.Identity.Auth.Sessions;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.CompleteMfaChallenge;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Mfa;

public class CompleteMfaChallengeTests : IdentityHandlerTestBase
{
    private const string Token = "challenge-token";
    private readonly Mock<IMfaChallengeStore> _store = new();
    private readonly Mock<IMfaCodeVerifier> _verifier = new();
    private readonly Mock<IAuthSessionIssuer> _issuer = new();
    private readonly MfaChallengePayload _challenge;

    public CompleteMfaChallengeTests()
    {
        _challenge = new MfaChallengePayload(
            Guid.NewGuid(), TestUserId, MfaChallengePurpose.PasswordLogin, TestNow, TestNow.AddMinutes(5));
        _store.Setup(s => s.PeekAsync(Token, It.IsAny<CancellationToken>())).ReturnsAsync(_challenge);
        _store.Setup(s => s.ConsumeAsync(Token, It.IsAny<CancellationToken>())).ReturnsAsync(_challenge);
        _store.Setup(s => s.RecordAttemptAsync(Token, MfaPolicy.ChallengeMaxAttempts, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaChallengeAttempt(1, false));
        ThrottleMock.Setup(t => t.BeginAttemptAsync(LoginThrottleScope.MfaChallenge, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginThrottleDecision.Allowed);
        SetupUsers(CreateUser());
        _issuer.Setup(i => i.IssueAsync(It.IsAny<User>(), TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthResult { AccessToken = "access", RefreshToken = "refresh" });
    }

    private CompleteMfaChallengeCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        _store.Object,
        _verifier.Object,
        ThrottleMock.Object,
        ActivityMock.Object,
        _issuer.Object,
        DateTimeProviderMock.Object,
        new Mock<ILogger<CompleteMfaChallengeCommandHandler>>().Object);

    private static CompleteMfaChallengeCommand Command(string code) =>
        new() { ChallengeToken = Token, Code = code };

    [Fact]
    public async Task Handle_WhenTheCodeIsRight_OpensTheSessionAndSpendsTheChallenge()
    {
        _verifier.Setup(v => v.VerifyAsync(TestUserId, "123456", TestNow, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateSut().Handle(Command("123456"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access");
        _store.Verify(s => s.ConsumeAsync(Token, It.IsAny<CancellationToken>()), Times.Once);
        ThrottleMock.Verify(t => t.RecordSuccessAsync(LoginThrottleScope.MfaChallenge, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheCodeIsWrong_IsRefusedCountsAsAFailedSignInAndOpensNothing()
    {
        _verifier.Setup(v => v.VerifyAsync(TestUserId, "000000", TestNow, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateSut().Handle(Command("000000"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.mfa.invalid-code");
        ActivityMock.Verify(a => a.Record(TestUserId, SecurityActivityType.SignInFailed, TestNow, It.IsAny<Guid?>()), Times.Once);
        ThrottleMock.Verify(t => t.RecordFailureAsync(LoginThrottleScope.MfaChallenge, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _issuer.Verify(i => i.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(s => s.ConsumeAsync(Token, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheFifthWrongCodeComesIn_KillsTheChallengeAtOnceAndAnswersExpired()
    {
        _store.Setup(s => s.RecordAttemptAsync(Token, MfaPolicy.ChallengeMaxAttempts, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaChallengeAttempt(MfaPolicy.ChallengeMaxAttempts, false));
        _verifier.Setup(v => v.VerifyAsync(TestUserId, "000000", TestNow, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CreateSut().Handle(Command("000000"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == CompleteMfaChallengeErrors.ChallengeInvalidCode);
        _store.Verify(s => s.ConsumeAsync(Token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheChallengeWasKilled_ARightCodeIsRefusedAndNeverVerified()
    {
        // The store forgot the challenge after the fifth wrong code: nothing to peek.
        _store.Setup(s => s.PeekAsync(Token, It.IsAny<CancellationToken>())).ReturnsAsync((MfaChallengePayload?)null);

        var result = await CreateSut().Handle(Command("123456"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == CompleteMfaChallengeErrors.ChallengeInvalidCode);
        _verifier.Verify(v => v.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _issuer.Verify(i => i.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        ThrottleMock.Verify(t => t.RecordAnonymousFailureAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenACodeWasAlreadyClaimed_TheVerifierRefusesItAndNoSecondSessionOpens()
    {
        // The code (or recovery code) already opened something: the verifier's one-time claim says no.
        _verifier.SetupSequence(v => v.VerifyAsync(TestUserId, "123456", TestNow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var sut = CreateSut();

        var first = await sut.Handle(Command("123456"), CancellationToken.None);
        var second = await sut.Handle(Command("123456"), CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeFalse();
        _issuer.Verify(i => i.IssueAsync(It.IsAny<User>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheChallengeIsForAStepUp_LoginRefusesItWithTheSameAnswerAsAnExpiredOne()
    {
        _store.Setup(s => s.PeekAsync(Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_challenge with { Purpose = MfaChallengePurpose.StepUpDisableMfa });

        var result = await CreateSut().Handle(Command("123456"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == CompleteMfaChallengeErrors.ChallengeInvalidCode);
        _verifier.Verify(v => v.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheAccountIsLocked_AnswersTheLockAndChecksNoCode()
    {
        ThrottleMock.Setup(t => t.BeginAttemptAsync(LoginThrottleScope.MfaChallenge, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginThrottleDecision.Locked(TimeSpan.FromMinutes(15)));

        var result = await CreateSut().Handle(Command("123456"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _verifier.Verify(v => v.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
