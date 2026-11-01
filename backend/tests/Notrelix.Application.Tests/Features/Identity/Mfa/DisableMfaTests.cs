using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Mfa.Commands.DisableMfa;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Mfa;

public class DisableMfaTests : IdentityHandlerTestBase
{
    public DisableMfaTests()
    {
        RequestContextMock.Setup(c => c.SessionId).Returns(TestSessionId);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("proof", TestUserId, TestSessionId, StepUpPurpose.DisableMfa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private DisableMfaCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        JwtBlacklistMock.Object,
        StepUpServiceMock.Object,
        DateTimeProviderMock.Object,
        ActivityMock.Object,
        new Mock<ILogger<DisableMfaCommandHandler>>().Object);

    private static UserMfaMethod ActiveMethod(Guid userId)
    {
        var method = UserMfaMethod.Create(userId, MfaMethodType.AuthenticatorApp, DateTimeOffset.UnixEpoch, SecretRef.Create("s"));
        method.Verify(DateTimeOffset.UnixEpoch);
        return method;
    }

    private static DisableMfaCommand Disable(string proof = "proof") => new() { StepUpToken = proof };

    private void NoteActivity(Times times) => ActivityMock.Verify(
        a => a.Record(It.IsAny<Guid>(), SecurityActivityType.MfaDisabled, It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), times);

    [Fact]
    public async Task WithNoActiveFactor_IsASuccessNoOp_ThatConsumesNothingAndRecordsNothing()
    {
        var session = CreateSession();
        SetupSessions(session);

        var result = await CreateSut().Handle(Disable(""), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Active);
        StepUpServiceMock.Verify(s => s.ConsumeAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        NoteActivity(Times.Never());
    }

    [Fact]
    public async Task WithoutAProof_IsRefusedWith412_AndTheFactorStays()
    {
        var method = ActiveMethod(TestUserId);
        SetupUserMfaMethods(method);

        var result = await CreateSut().Handle(Disable(""), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        method.Status.Should().Be(MfaMethodStatus.Active);
        NoteActivity(Times.Never());
    }

    [Fact]
    public async Task AWrongSpentOrOtherPurposeProof_IsRefused_AndTheFactorAndSessionsStay()
    {
        var method = ActiveMethod(TestUserId);
        var session = CreateSession();
        SetupUserMfaMethods(method);
        SetupSessions(session);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("bad", TestUserId, TestSessionId, StepUpPurpose.DisableMfa, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError(
                "identity.security.step-up-invalid", "Invalid.", ApplicationErrorType.Validation)));

        var result = await CreateSut().Handle(Disable("bad"), CancellationToken.None);

        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        method.Status.Should().Be(MfaMethodStatus.Active);
        session.Status.Should().Be(SessionStatus.Active);
        NoteActivity(Times.Never());
    }

    [Fact]
    public async Task WithAProof_TurnsTheFactorOff_VoidsTheCodes_EndsEverySession_AndRecordsOneLine()
    {
        var method = ActiveMethod(TestUserId);
        var batch = MfaRecoveryBatch.Create(TestUserId, ["h1", "h2"], TestNow, TestUserId);
        var one = CreateSession();
        var two = CreateSession();
        SetupUserMfaMethods(method);
        SetupMfaRecoveryBatches(batch);
        SetupSessions(one, two);

        var result = await CreateSut().Handle(Disable(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        method.Status.Should().Be(MfaMethodStatus.Disabled);
        batch.InvalidatedAt.Should().NotBeNull();
        one.Status.Should().Be(SessionStatus.Revoked);
        two.Status.Should().Be(SessionStatus.Revoked);
        JwtBlacklistMock.Verify(b => b.RevokeUserBeforeAsync(TestUserId, TestNow, It.IsAny<TimeSpan>()), Times.Once);
        NoteActivity(Times.Once());
    }

    [Fact]
    public async Task AnotherPersonsFactorAndSessions_AreNeverTouched()
    {
        var mine = ActiveMethod(TestUserId);
        var otherUser = Guid.CreateVersion7();
        var theirs = ActiveMethod(otherUser);
        var theirSession = CreateSession(userId: otherUser);
        SetupUserMfaMethods(mine, theirs);
        SetupSessions(theirSession);

        await CreateSut().Handle(Disable(), CancellationToken.None);

        theirs.Status.Should().Be(MfaMethodStatus.Active);
        theirSession.Status.Should().Be(SessionStatus.Active);
    }
}
