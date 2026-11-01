using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Identity.Sessions.Commands.RevokeSession;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Sessions;

public class RevokeSessionTests : IdentityHandlerTestBase
{
    private RevokeSessionCommandHandler CreateSut() => new(
        IdentityContextMock.Object,
        RequestContextMock.Object,
        JwtBlacklistMock.Object,
        DateTimeProviderMock.Object,
        ActivityMock.Object,
        new Mock<ILogger<RevokeSessionCommandHandler>>().Object);

    private static RevokeSessionCommand Revoke(Guid id) => new() { SessionId = id };

    [Fact]
    public async Task AnOwnActiveSession_IsRevokedCutOffAndRecordedOnce()
    {
        var session = CreateSession();
        SetupSessions(session);

        var result = await CreateSut().Handle(Revoke(session.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Revoked);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(session.Id, TestNow, It.IsAny<TimeSpan>()), Times.Once);
        ActivityMock.Verify(a => a.Record(TestUserId, SecurityActivityType.SessionRevoked, TestNow, session.Id), Times.Once);
    }

    [Fact]
    public async Task AnAlreadyRevokedSession_IsASuccessNoOp_WithNoSecondLineOrBlacklistWrite()
    {
        var session = CreateSession(status: SessionStatus.Revoked);
        SetupSessions(session);

        var result = await CreateSut().Handle(Revoke(session.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Revoked);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        ActivityMock.Verify(a => a.Record(It.IsAny<Guid>(), It.IsAny<SecurityActivityType>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task AnExpiredSession_IsASuccessNoOp_NotARuleError()
    {
        var session = CreateSession();
        session.Expire(TestNow);
        SetupSessions(session);

        var result = await CreateSut().Handle(Revoke(session.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Expired);
        ActivityMock.Verify(a => a.Record(It.IsAny<Guid>(), It.IsAny<SecurityActivityType>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task AnotherPersonsSession_AndAnUnknownId_AnswerTheSameNotFound_AndNothingChanges()
    {
        var foreign = CreateSession(userId: Guid.CreateVersion7());
        SetupSessions(foreign);

        var foreignResult = await CreateSut().Handle(Revoke(foreign.Id), CancellationToken.None);
        var unknownResult = await CreateSut().Handle(Revoke(Guid.CreateVersion7()), CancellationToken.None);

        foreignResult.Succeeded.Should().BeFalse();
        unknownResult.Succeeded.Should().BeFalse();
        foreignResult.TypedErrors.Should().BeEquivalentTo(unknownResult.TypedErrors);
        foreignResult.TypedErrors.Should().ContainSingle(e =>
            e.Code == "identity.session.not-found" && e.Type == ApplicationErrorType.NotFound);
        foreign.Status.Should().Be(SessionStatus.Active);
        JwtBlacklistMock.Verify(b => b.RevokeSessionBeforeAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<TimeSpan>()), Times.Never);
        ActivityMock.Verify(a => a.Record(It.IsAny<Guid>(), It.IsAny<SecurityActivityType>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task RevokingTheCurrentSession_EndsIt()
    {
        var session = CreateSession();
        SetupSessions(session);
        RequestContextMock.Setup(c => c.SessionId).Returns(session.Id);

        var result = await CreateSut().Handle(Revoke(session.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        session.Status.Should().Be(SessionStatus.Revoked);
    }
}
