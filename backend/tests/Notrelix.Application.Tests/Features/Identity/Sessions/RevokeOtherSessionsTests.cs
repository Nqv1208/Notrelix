using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Security.DTOs;
using Notrelix.Application.Features.Identity.Sessions.Commands.RevokeOtherSessions;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Sessions;

public class RevokeOtherSessionsTests : IdentityHandlerTestBase
{
    private readonly UserSession _current;
    private readonly UserSession _other1;
    private readonly UserSession _other2;

    public RevokeOtherSessionsTests()
    {
        _current = CreateSession();
        _other1 = CreateSession();
        _other2 = CreateSession();
        RequestContextMock.Setup(c => c.SessionId).Returns(_current.Id);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("proof", TestUserId, _current.Id, StepUpPurpose.RevokeOtherSessions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
    }

    private RevokeOtherSessionsCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        JwtBlacklistMock.Object,
        StepUpServiceMock.Object,
        DateTimeProviderMock.Object,
        ActivityMock.Object,
        new Mock<ILogger<RevokeOtherSessionsCommandHandler>>().Object);

    private void NothingChanged()
    {
        _current.Status.Should().Be(SessionStatus.Active);
        _other1.Status.Should().Be(SessionStatus.Active);
        _other2.Status.Should().Be(SessionStatus.Active);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        ActivityMock.Verify(a => a.Record(It.IsAny<Guid>(), It.IsAny<SecurityActivityType>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task WithAProof_EndsEveryOtherSession_KeepsTheCurrentOne_AndRecordsOnce()
    {
        SetupSessions(_current, _other1, _other2);

        var result = await CreateSut().Handle(new RevokeOtherSessionsCommand { StepUpToken = "proof" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.RevokedCount.Should().Be(2);
        _current.Status.Should().Be(SessionStatus.Active);
        _other1.Status.Should().Be(SessionStatus.Revoked);
        _other2.Status.Should().Be(SessionStatus.Revoked);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(_other1.Id, TestNow, It.IsAny<TimeSpan>()), Times.Once);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(_other2.Id, TestNow, It.IsAny<TimeSpan>()), Times.Once);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(_current.Id, It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        ActivityMock.Verify(a => a.Record(TestUserId, SecurityActivityType.OtherSessionsRevoked, TestNow, _current.Id), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task WithoutAProof_IsRefusedAsPreconditionFailed_AndEveryDeviceStays(string token)
    {
        SetupSessions(_current, _other1, _other2);

        var result = await CreateSut().Handle(new RevokeOtherSessionsCommand { StepUpToken = token }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.security.step-up-required" && e.Type == ApplicationErrorType.PreconditionFailed);
        NothingChanged();
    }

    [Fact]
    public async Task AWrongOrSpentProof_IsRefused_AndEveryDeviceStays()
    {
        SetupSessions(_current, _other1, _other2);
        StepUpServiceMock
            .Setup(s => s.ConsumeAsync("spent", TestUserId, _current.Id, StepUpPurpose.RevokeOtherSessions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(new ApplicationError("identity.security.step-up-invalid", "Invalid proof.", ApplicationErrorType.Validation)));

        var result = await CreateSut().Handle(new RevokeOtherSessionsCommand { StepUpToken = "spent" }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e => e.Code == "identity.security.step-up-invalid");
        NothingChanged();
    }

    [Fact]
    public async Task WithNoOtherSession_IsASuccessNoOp_ThatConsumesNoProofAndRecordsNothing()
    {
        SetupSessions(_current);

        var result = await CreateSut().Handle(new RevokeOtherSessionsCommand { StepUpToken = "proof" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.RevokedCount.Should().Be(0);
        StepUpServiceMock.Verify(s => s.ConsumeAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<StepUpPurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        ActivityMock.Verify(a => a.Record(It.IsAny<Guid>(), It.IsAny<SecurityActivityType>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task WhenTheCurrentSessionIsNotBound_AnswersConflict_AndChangesNothing()
    {
        SetupSessions(_current, _other1);
        RequestContextMock.Setup(c => c.SessionId).Returns((Guid?)null);

        var result = await CreateSut().Handle(new RevokeOtherSessionsCommand { StepUpToken = "proof" }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.session.not-bound" && e.Type == ApplicationErrorType.Conflict);
        _other1.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task AnotherPersonsSessions_AndEndedOnes_AreNeverTouchedOrCounted()
    {
        var foreign = CreateSession(userId: Guid.CreateVersion7());
        var alreadyRevoked = CreateSession(status: SessionStatus.Revoked);
        SetupSessions(_current, _other1, foreign, alreadyRevoked);

        var result = await CreateSut().Handle(new RevokeOtherSessionsCommand { StepUpToken = "proof" }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.RevokedCount.Should().Be(1);
        foreign.Status.Should().Be(SessionStatus.Active);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(foreign.Id, It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(alreadyRevoked.Id, It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
    }
}
