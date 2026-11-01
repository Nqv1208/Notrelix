using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa;
using Notrelix.Application.Features.Identity.Mfa.Abstractions;
using Notrelix.Application.Features.Identity.Mfa.Commands.VerifyMfaEnrollment;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;

namespace Notrelix.Application.Tests.Features.Identity.Mfa;

public class VerifyMfaEnrollmentTests : IdentityHandlerTestBase
{
    private const string Code = "123456";
    private readonly Mock<IMfaTotpService> _totp = new();
    private readonly Mock<IMfaRecoveryCodeGenerator> _recovery = new();

    public VerifyMfaEnrollmentTests()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        _totp.Setup(t => t.UnprotectSecret(It.IsAny<string>())).Returns("secret");
        _totp.Setup(t => t.VerifyCode("secret", Code, It.IsAny<DateTimeOffset>())).Returns(true);
        _recovery.Setup(r => r.Generate(MfaPolicy.RecoveryCodeCount))
            .Returns(Enumerable.Range(0, MfaPolicy.RecoveryCodeCount).Select(i => $"code-{i}").ToArray());
        _recovery.Setup(r => r.Hash(It.IsAny<string>())).Returns((string c) => $"hash-{c}");
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(It.IsAny<string>(), TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private VerifyMfaEnrollmentCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        _totp.Object,
        _recovery.Object,
        StepUpServiceMock.Object,
        JwtBlacklistMock.Object,
        DateTimeProviderMock.Object,
        ActivityMock.Object,
        new Mock<ILogger<VerifyMfaEnrollmentCommandHandler>>().Object);

    private UserMfaMethod Pending()
    {
        var method = UserMfaMethod.Create(TestUserId, MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("protected"));
        method.GetType().GetProperty(nameof(UserMfaMethod.UserId))!.SetValue(method, TestUserId);
        return method;
    }

    private UserMfaMethod Active()
    {
        var method = Pending();
        method.Verify(TestNow);
        return method;
    }

    [Fact]
    public async Task WithoutAProof_IsRefusedAndNothingIsActivated()
    {
        var pending = Pending();
        SetupUserMfaMethods(pending);

        var result = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = pending.Id, Code = Code }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        pending.Status.Should().Be(MfaMethodStatus.PendingVerification);
        _totp.Verify(t => t.VerifyCode(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
    }

    [Fact]
    public async Task WhenAnotherMethodIsAlreadyActive_IsRefusedAndTheProofIsKept()
    {
        var active = Active();
        var pending = Pending();
        SetupUserMfaMethods(active, pending);

        var result = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = pending.Id, Code = Code, StepUpToken = "proof" }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.mfa.already-enabled");
        pending.Status.Should().Be(MfaMethodStatus.PendingVerification);
        StepUpServiceMock.Verify(s => s.ConsumeAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenNoMethodIsActive_ActivatesThePendingOne()
    {
        var pending = Pending();
        SetupUserMfaMethods(pending);

        var result = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = pending.Id, Code = Code, StepUpToken = "proof" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        pending.Status.Should().Be(MfaMethodStatus.Active);
    }

    [Fact]
    public async Task AWrongCode_AnswersInvalidCode_LeavesThePendingMethod_AndKeepsTheProof()
    {
        var pending = Pending();
        SetupUserMfaMethods(pending);

        var result = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = pending.Id, Code = "000000", StepUpToken = "proof" }, CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.mfa.invalid-verification-code" && e.Type == ApplicationErrorType.Validation);
        pending.Status.Should().Be(MfaMethodStatus.PendingVerification);
        StepUpServiceMock.Verify(s => s.ConsumeAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AnotherPersonsMethodId_AndAnUnknownId_AnswerTheSameRefusal_AndNothingChanges()
    {
        var theirs = UserMfaMethod.Create(Guid.CreateVersion7(), MfaMethodType.AuthenticatorApp, TestNow, SecretRef.Create("theirs"));
        SetupUserMfaMethods(theirs);

        var foreign = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = theirs.Id, Code = Code, StepUpToken = "proof" }, CancellationToken.None);
        var unknown = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = Guid.CreateVersion7(), Code = Code, StepUpToken = "proof" }, CancellationToken.None);

        foreign.TypedErrors.Should().BeEquivalentTo(unknown.TypedErrors);
        foreign.TypedErrors.Should().ContainSingle(e => e.Code == "identity.mfa.enrollment-not-found");
        theirs.Status.Should().Be(MfaMethodStatus.PendingVerification);
    }

    [Fact]
    public async Task VerifyingAnAlreadyActiveMethod_AnswersTheSameRefusal_SoCodesCannotBeReplayed()
    {
        var active = Active();
        SetupUserMfaMethods(active);

        var result = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = active.Id, Code = Code, StepUpToken = "proof" }, CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.mfa.enrollment-not-found");
    }

    [Fact]
    public async Task AWrongOrSpentProof_AfterARightCode_ActivatesNothingAndIssuesNoCodes()
    {
        var pending = Pending();
        SetupUserMfaMethods(pending);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("spent", TestUserId, TestSessionId, StepUpPurpose.EnrollMfa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError(
                "identity.security.step-up-invalid", "Invalid.", ApplicationErrorType.Validation)));

        var result = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = pending.Id, Code = Code, StepUpToken = "spent" }, CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        pending.Status.Should().Be(MfaMethodStatus.PendingVerification);
        IdentityContextMock.Object.MfaRecoveryBatches.Should().BeEmpty();
    }

    [Fact]
    public async Task Activating_IssuesEightCodes_EndsOtherSessionsOnly_AndRecordsTheLines()
    {
        var pending = Pending();
        var current = CreateSession();
        var other = CreateSession();
        SetupUserMfaMethods(pending);
        SetupSessions(current, other);
        RequestContextMock.Setup(c => c.SessionId).Returns(current.Id);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync(It.IsAny<string>(), TestUserId, current.Id, StepUpPurpose.EnrollMfa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await CreateSut().Handle(
            new VerifyMfaEnrollmentCommand { MfaMethodId = pending.Id, Code = Code, StepUpToken = "proof" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.RecoveryCodes.Should().HaveCount(8);
        result.Data.OtherSessionsEnded.Should().Be(1);
        current.Status.Should().Be(Notrelix.Domain.Identity.Sessions.SessionStatus.Active);
        other.Status.Should().Be(Notrelix.Domain.Identity.Sessions.SessionStatus.Revoked);
        ActivityMock.Verify(a => a.Record(TestUserId, Notrelix.Domain.Identity.Security.SecurityActivityType.MfaEnabled, TestNow, current.Id), Times.Once);
        ActivityMock.Verify(a => a.Record(TestUserId, Notrelix.Domain.Identity.Security.SecurityActivityType.OtherSessionsRevoked, TestNow, current.Id), Times.Once);
    }
}
