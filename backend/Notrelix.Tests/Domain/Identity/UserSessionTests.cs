using FluentAssertions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Sessions;
using Notrelix.Domain.Identity.Credentials;
using Xunit;

namespace Notrelix.Domain.Tests.Identity;

public class UserSessionTests
{
    private static readonly RefreshTokenHash ValidTokenHash = RefreshTokenHash.Create("test-refresh-token");

    [Fact]
    public void Create_ShouldRaiseSessionCreatedEvent_WithCorrectIds()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var session = UserSession.Create(userId, ValidTokenHash, now.AddDays(30), now);

        session.UserId.Should().Be(userId);
        session.Status.Should().Be(SessionStatus.Active);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionCreatedEvent);
        var evt = (UserSessionCreatedEvent)session.DomainEvents.First(e => e is UserSessionCreatedEvent);
        evt.UserId.Should().Be(userId);
        evt.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public void Create_ShouldSetCreatedAt()
    {
        var now = DateTimeOffset.UtcNow;

        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);

        session.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Revoke_ShouldChangeStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.ClearDomainEvents();

        var revokeTime = now.AddDays(1);
        session.Revoke(revokeTime);

        session.Status.Should().Be(SessionStatus.Revoked);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionRevokedEvent);
        var evt = (UserSessionRevokedEvent)session.DomainEvents.First(e => e is UserSessionRevokedEvent);
        evt.SessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(session.UserId);
    }

    [Fact]
    public void Revoke_AlreadyRevoked_ShouldNotRaiseEventAgain()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Revoke(now.AddDays(1));
        session.ClearDomainEvents();

        session.Revoke(now.AddDays(2));

        session.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Revoke_ShouldSetUpdatedAt()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);

        var revokeTime = now.AddDays(1);
        session.Revoke(revokeTime);

        session.UpdatedAt.Should().Be(revokeTime);
    }

    [Fact]
    public void Session_ShouldExtendAggregateRoot()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);

        session.Should().BeAssignableTo<AggregateRoot>();
    }
}
