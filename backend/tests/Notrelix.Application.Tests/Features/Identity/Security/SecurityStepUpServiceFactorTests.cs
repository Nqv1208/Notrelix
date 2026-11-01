using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Auth.Throttling;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.DTOs;
using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Security.Queries.GetStepUpRequirement;
using Notrelix.Application.Features.Identity.Security.Services;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Users;

namespace Notrelix.Application.Tests.Features.Identity.Security;

/// <summary>Which step-up factor applies (K03), and the code (K04) and password (K05) answers.</summary>
public class SecurityStepUpServiceFactorTests : IdentityHandlerTestBase
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

    private void WithActiveFactor()
    {
        var method = UserMfaMethod.Create(TestUserId, MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("s"));
        method.Verify(TestNow);
        SetupUserMfaMethods(method);
    }

    private MfaChallengePayload Challenge(Guid? user = null, Guid? session = null, MfaChallengePurpose? purpose = null) => new(
        Guid.CreateVersion7(), user ?? TestUserId, purpose ?? MfaChallengePurpose.StepUpDisableMfa,
        TestNow, TestNow.AddMinutes(5), session ?? TestSessionId);

    private Task<Result<StepUpProofResult>> CompleteMfa(string token = "tok") => CreateSut().CompleteMfaAsync(
        TestUserId, TestSessionId, StepUpPurpose.DisableMfa, token, "123456", CancellationToken.None);

    [Fact]
    public async Task Requirement_WithAnActiveFactor_IsMfa_AndIssuesAChallengeBoundToTheSession()
    {
        WithActiveFactor();
        SetupUsers(CreateUser());

        var result = await CreateSut().GetRequirementAsync(TestUserId, TestSessionId, StepUpPurpose.DisableMfa, CancellationToken.None);

        result.Data!.RequiredFactor.Should().Be(StepUpRequiredFactor.MfaChallenge);
        result.Data.ChallengeToken.Should().NotBeNullOrEmpty();
        ChallengeStoreMock.Verify(s => s.StoreAsync(
            result.Data.ChallengeToken!,
            It.Is<MfaChallengePayload>(p => p.UserId == TestUserId && p.SessionId == TestSessionId && p.Purpose == MfaChallengePurpose.StepUpDisableMfa),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Requirement_WithAPasswordAndNoFactor_IsPassword_AndIssuesNoChallenge()
    {
        SetupUsers(CreateUser());

        var result = await CreateSut().GetRequirementAsync(TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, CancellationToken.None);

        result.Data!.RequiredFactor.Should().Be(StepUpRequiredFactor.Password);
        result.Data.ChallengeToken.Should().BeNull();
        ChallengeStoreMock.Verify(s => s.StoreAsync(It.IsAny<string>(), It.IsAny<MfaChallengePayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Requirement_ForAProviderOnlyPerson_IsProviderReauthentication()
    {
        var user = User.Create(TestEmail, "Test", "sentinel-hash", TestNow, hasPasswordCredential: false);
        user.GetType().GetProperty(nameof(User.Id))!.SetValue(user, TestUserId);
        SetupUsers(user);

        var result = await CreateSut().GetRequirementAsync(TestUserId, TestSessionId, StepUpPurpose.DisableMfa, CancellationToken.None);

        result.Data!.RequiredFactor.Should().Be(StepUpRequiredFactor.OAuth);
    }

    [Fact]
    public async Task Requirement_ForAnotherPersonsActiveFactor_DoesNotMakeMineMfa()
    {
        var theirs = UserMfaMethod.Create(Guid.CreateVersion7(), MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("s"));
        theirs.Verify(TestNow);
        SetupUserMfaMethods(theirs);
        SetupUsers(CreateUser());

        var result = await CreateSut().GetRequirementAsync(TestUserId, TestSessionId, StepUpPurpose.DisableMfa, CancellationToken.None);

        result.Data!.RequiredFactor.Should().Be(StepUpRequiredFactor.Password);
    }

    [Fact]
    public async Task RequirementQuery_WithoutASession_IsRefusedWith412_AndAsksNothing()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns((Guid?)null);
        var sut = new GetStepUpRequirementQueryHandler(StepUpServiceMock.Object, RequestContextMock.Object);

        var result = await sut.Handle(new GetStepUpRequirementQuery { Purpose = StepUpPurpose.DisableMfa }, CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        StepUpServiceMock.Verify(s => s.GetRequirementAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteMfa_WithTheRightCode_IssuesOneProof_AndClearsTheBudget()
    {
        var challenge = Challenge();
        ChallengeStoreMock.Setup(s => s.PeekAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        ChallengeStoreMock.Setup(s => s.ConsumeAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        ChallengeStoreMock.Setup(s => s.RecordAttemptAsync("tok", It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaChallengeAttempt(1, false));
        _codeVerifier.Setup(v => v.VerifyAsync(TestUserId, "123456", TestNow, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CompleteMfa();

        result.Succeeded.Should().BeTrue();
        _proofStore.Verify(p => p.StoreAsync(result.Data!.ProofToken,
            It.Is<StepUpProofPayload>(x => x.UserId == TestUserId && x.SessionId == TestSessionId && x.Purpose == StepUpPurpose.DisableMfa),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        ThrottleMock.Verify(t => t.RecordSuccessAsync(LoginThrottleScope.StepUpMfa, TestUserId.ToString("N"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteMfa_WithAWrongCode_AnswersInvalidCode_KeepsTheChallenge_AndIssuesNoProof()
    {
        ChallengeStoreMock.Setup(s => s.PeekAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(Challenge());
        ChallengeStoreMock.Setup(s => s.RecordAttemptAsync("tok", It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaChallengeAttempt(1, false));
        _codeVerifier.Setup(v => v.VerifyAsync(TestUserId, It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CompleteMfa();

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-invalid-code" && e.Type == ApplicationErrorType.Validation);
        ChallengeStoreMock.Verify(s => s.ConsumeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        ThrottleMock.Verify(t => t.RecordFailureAsync(LoginThrottleScope.StepUpMfa, TestUserId.ToString("N"), It.IsAny<CancellationToken>()), Times.Once);
        _proofStore.Verify(p => p.StoreAsync(It.IsAny<string>(), It.IsAny<StepUpProofPayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompleteMfa_TheFifthWrongCode_KillsTheChallenge_AndAnswersExpired()
    {
        ChallengeStoreMock.Setup(s => s.PeekAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(Challenge());
        ChallengeStoreMock.Setup(s => s.RecordAttemptAsync("tok", It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaChallengeAttempt(MfaPolicy.ChallengeMaxAttempts, false));
        _codeVerifier.Setup(v => v.VerifyAsync(TestUserId, It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await CompleteMfa();

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        ChallengeStoreMock.Verify(s => s.ConsumeAsync("tok", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("other-user")]
    [InlineData("other-session")]
    [InlineData("other-purpose")]
    [InlineData("expired")]
    [InlineData("unknown")]
    public async Task CompleteMfa_WithAChallengeForAnotherUserSessionPurposeOrAnExpiredOne_IsRefused_BeforeAnyCodeIsChecked(string kind)
    {
        MfaChallengePayload? challenge = kind switch
        {
            "other-user" => Challenge(user: Guid.CreateVersion7()),
            "other-session" => Challenge(session: Guid.CreateVersion7()),
            "other-purpose" => Challenge(purpose: MfaChallengePurpose.StepUpChangePassword),
            "expired" => Challenge() with { ExpiresAt = TestNow.AddSeconds(-1) },
            _ => null,
        };
        ChallengeStoreMock.Setup(s => s.PeekAsync("tok", It.IsAny<CancellationToken>())).ReturnsAsync(challenge);

        var result = await CompleteMfa();

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        _codeVerifier.Verify(v => v.VerifyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _proofStore.Verify(p => p.StoreAsync(It.IsAny<string>(), It.IsAny<StepUpProofPayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletePassword_ForAProviderOnlyPerson_IsRefused_AndIssuesNoProof()
    {
        var user = User.Create(TestEmail, "Test", "sentinel-hash", TestNow, hasPasswordCredential: false);
        user.GetType().GetProperty(nameof(User.Id))!.SetValue(user, TestUserId);
        SetupUsers(user);

        var result = await CreateSut().CompletePasswordAsync(
            TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, "anything", CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        _proofStore.Verify(p => p.StoreAsync(It.IsAny<string>(), It.IsAny<StepUpProofPayload>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CompletePassword_WithTheRightPassword_BindsTheProofToUserSessionAndPurpose()
    {
        SetupUsers(CreateUser());
        PasswordHasherMock.Setup(h => h.VerifyPassword(TestPassword, TestHashedPassword)).Returns(true);

        var result = await CreateSut().CompletePasswordAsync(
            TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, TestPassword, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _proofStore.Verify(p => p.StoreAsync(result.Data!.ProofToken,
            It.Is<StepUpProofPayload>(x => x.UserId == TestUserId && x.SessionId == TestSessionId && x.Purpose == StepUpPurpose.EnrollMfa),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompletePassword_WithAWrongPassword_AnswersInvalidPassword()
    {
        SetupUsers(CreateUser());
        PasswordHasherMock.Setup(h => h.VerifyPassword("bad", TestHashedPassword)).Returns(false);

        var result = await CreateSut().CompletePasswordAsync(
            TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, "bad", CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-invalid-password" && e.Type == ApplicationErrorType.Validation);
    }
}
