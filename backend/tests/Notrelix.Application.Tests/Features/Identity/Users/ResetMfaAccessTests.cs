using Notrelix.Application.Features.Identity.Security.Abstractions;
using Notrelix.Application.Features.Identity.Users.Services;
using Notrelix.Domain.Identity.Mfa;
using Notrelix.Domain.Identity.Security;
using Notrelix.Domain.Identity.Sessions;

namespace Notrelix.Application.Tests.Features.Identity.Users;

public class ResetMfaAccessTests : IdentityHandlerTestBase
{
    private IdentityUserAccessProvider CreateSut() => new(
        IdentityContextMock.Object, JwtBlacklistMock.Object, DateTimeProviderMock.Object, ActivityMock.Object, SecurityNoticeMock.Object);

    private static UserMfaMethod ActiveMethod(Guid userId)
    {
        var method = UserMfaMethod.Create(userId, MfaMethodType.AuthenticatorApp, DateTimeOffset.UnixEpoch, SecretRef.Create("s"));
        method.Verify(DateTimeOffset.UnixEpoch);
        return method;
    }

    [Fact]
    public async Task ResetsFactorsCodesAndSessions_RecordsOneLine_AndMailsThePerson()
    {
        var target = Guid.CreateVersion7();
        var other = Guid.CreateVersion7();
        var method = ActiveMethod(target);
        var theirs = ActiveMethod(other);
        var batch = MfaRecoveryBatch.Create(target, ["h1"], TestNow, target);
        var session = CreateSession(userId: target);
        var otherSession = CreateSession(userId: other);
        SetupUsers(CreateUser(id: target));
        SetupUserMfaMethods(method, theirs);
        SetupMfaRecoveryBatches(batch);
        SetupSessions(session, otherSession);

        var found = await CreateSut().ResetMfaAsync(target, TestUserId, CancellationToken.None);

        found.Should().BeTrue();
        method.Status.Should().Be(MfaMethodStatus.Disabled);
        batch.InvalidatedAt.Should().NotBeNull();
        session.Status.Should().Be(SessionStatus.Revoked);
        theirs.Status.Should().Be(MfaMethodStatus.Active);
        otherSession.Status.Should().Be(SessionStatus.Active);
        JwtBlacklistMock.Verify(b => b.RevokeUserBeforeAsync(target, TestNow, It.IsAny<TimeSpan>()), Times.Once);
        ActivityMock.Verify(a => a.Record(target, SecurityActivityType.MfaReset, TestNow, It.IsAny<Guid?>()), Times.Once);
        SecurityNoticeMock.Verify(n => n.QueueAsync(target, SecurityNoticeKind.MfaReset, TestNow, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnUnknownPerson_ChangesNothing()
    {
        SetupUsers();

        (await CreateSut().ResetMfaAsync(Guid.CreateVersion7(), TestUserId, CancellationToken.None)).Should().BeFalse();

        ActivityMock.Verify(a => a.Record(It.IsAny<Guid>(), It.IsAny<SecurityActivityType>(), It.IsAny<DateTimeOffset>(), It.IsAny<Guid?>()), Times.Never);
    }
}
