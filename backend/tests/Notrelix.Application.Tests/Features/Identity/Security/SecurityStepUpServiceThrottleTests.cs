using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Services;

namespace Notrelix.Application.Tests.Features.Identity.Security;

/// <summary>
/// Step-up completion is a credential-guessing surface even for a signed-in user: both factors
/// must reserve an attempt before verifying and report the outcome to the throttle.
/// </summary>
public class SecurityStepUpServiceThrottleTests : IdentityHandlerTestBase
{
    private readonly Mock<IStepUpProofStore> _proofStore = new();
    private readonly Mock<IMfaCodeVerifier> _codeVerifier = new();

    private SecurityStepUpService CreateSut() => new(
        IdentityContextMock.Object,
        ChallengeStoreMock.Object,
        _proofStore.Object,
        _codeVerifier.Object,
        PasswordHasherMock.Object,
        DateTimeProviderMock.Object,
        ThrottleMock.Object);

    private void Lock(LoginThrottleScope scope) =>
        ThrottleMock
            .Setup(t => t.BeginAttemptAsync(scope, TestUserId.ToString("N"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LoginThrottleDecision.Locked(TimeSpan.FromMinutes(3)));

    [Fact]
    public async Task CompleteMfa_WhenLocked_ReturnsTooManyAttemptsWithoutTouchingTheChallengeOrCode()
    {
        Lock(LoginThrottleScope.StepUpMfa);

        var result = await CreateSut().CompleteMfaAsync(
            TestUserId, TestSessionId, StepUpPurpose.DisableMfa, "challenge", "123456", CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == LoginThrottleErrors.Code && e.Type == ApplicationErrorType.TooManyRequests && e.RetryAfter == TimeSpan.FromMinutes(3));
        ChallengeStoreMock.Verify(s => s.PeekAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _codeVerifier.Verify(v => v.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteMfa_WhenChallengeUnknown_CountsAFailure()
    {
        ChallengeStoreMock.Setup(s => s.PeekAsync("nope", It.IsAny<CancellationToken>())).ReturnsAsync((MfaChallengePayload?)null);

        var result = await CreateSut().CompleteMfaAsync(
            TestUserId, TestSessionId, StepUpPurpose.DisableMfa, "nope", "123456", CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        ThrottleMock.Verify(t => t.RecordFailureAsync(LoginThrottleScope.StepUpMfa, TestUserId.ToString("N"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompletePassword_WhenLocked_ReturnsTooManyAttemptsWithoutVerifyingThePassword()
    {
        SetupUsers(CreateUser());
        Lock(LoginThrottleScope.PasswordReverification);

        var result = await CreateSut().CompletePasswordAsync(
            TestUserId, TestSessionId, StepUpPurpose.DisableMfa, TestPassword, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == LoginThrottleErrors.Code);
        PasswordHasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _proofStore.Verify(p => p.StoreAsync(It.IsAny<string>(), It.IsAny<StepUpProofPayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletePassword_WhenWrong_CountsAFailureAndIssuesNoProof()
    {
        SetupUsers(CreateUser());
        PasswordHasherMock.Setup(h => h.VerifyPassword("bad", TestHashedPassword)).Returns(false);

        var result = await CreateSut().CompletePasswordAsync(
            TestUserId, TestSessionId, StepUpPurpose.DisableMfa, "bad", CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        ThrottleMock.Verify(t => t.RecordFailureAsync(LoginThrottleScope.PasswordReverification, TestUserId.ToString("N"), It.IsAny<CancellationToken>()), Times.Once);
        _proofStore.Verify(p => p.StoreAsync(It.IsAny<string>(), It.IsAny<StepUpProofPayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletePassword_WhenRight_ClearsTheBudgetAndIssuesAProof()
    {
        SetupUsers(CreateUser());
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);

        var result = await CreateSut().CompletePasswordAsync(
            TestUserId, TestSessionId, StepUpPurpose.DisableMfa, TestPassword, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        ThrottleMock.Verify(t => t.RecordSuccessAsync(LoginThrottleScope.PasswordReverification, TestUserId.ToString("N"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
