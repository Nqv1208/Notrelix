using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity;

[CoversAggregate(typeof(UserSession))]
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
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionCreatedDomainEvent);
        var evt = (UserSessionCreatedDomainEvent)session.DomainEvents.First(e => e is UserSessionCreatedDomainEvent);
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

    [CoversMutation(typeof(UserSession), "Revoke(System.DateTimeOffset,System.String)", MutationScenario.Event)]
    [Fact]
    public void Revoke_ShouldChangeStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        ((IHasDomainEvents)session).ClearDomainEvents();

        var revokeTime = now.AddDays(1);
        session.Revoke(revokeTime);

        session.Status.Should().Be(SessionStatus.Revoked);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionRevokedDomainEvent);
        var evt = (UserSessionRevokedDomainEvent)session.DomainEvents.First(e => e is UserSessionRevokedDomainEvent);
        evt.SessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(session.UserId);
    }

    [CoversMutation(typeof(UserSession), "Revoke(System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void Revoke_AlreadyRevoked_ShouldNotRaiseEventAgain()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Revoke(now.AddDays(1));
        ((IHasDomainEvents)session).ClearDomainEvents();

        session.Revoke(now.AddDays(2));

        session.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(UserSession), "Revoke(System.DateTimeOffset,System.String)", MutationScenario.Valid)]
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
    public void Create_WithInvalidExpiration_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var act = () => UserSession.Create(Guid.NewGuid(), ValidTokenHash, now, now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*after creation time*");
    }

    [CoversMutation(typeof(UserSession), "Expire(System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Expire_ShouldChangeStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        ((IHasDomainEvents)session).ClearDomainEvents();

        var expireTime = now.AddDays(1);
        session.Expire(expireTime);

        session.Status.Should().Be(SessionStatus.Expired);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionExpiredDomainEvent);
        var evt = (UserSessionExpiredDomainEvent)session.DomainEvents.First(e => e is UserSessionExpiredDomainEvent);
        evt.SessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(session.UserId);
        evt.ExpiredAt.Should().Be(expireTime);
    }

    [CoversMutation(typeof(UserSession), "Expire(System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void Expire_AlreadyExpired_ShouldBeIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Expire(now.AddDays(1));
        ((IHasDomainEvents)session).ClearDomainEvents();

        session.Expire(now.AddDays(2));

        session.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(UserSession), "Expire(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Revoke_OnExpiredSession_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Expire(now.AddDays(1));

        var act = () => session.Revoke(now.AddDays(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*expired session*");
    }

    [CoversMutation(typeof(UserSession), "Expire(System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Expire_OnRevokedSession_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Revoke(now.AddDays(1));

        var act = () => session.Expire(now.AddDays(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*revoked session*");
    }

    [CoversMutation(typeof(UserSession), "UpdateRefreshToken(Notrelix.Domain.Identity.Sessions.RefreshTokenHash,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void UpdateRefreshToken_ShouldRotateTokenAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        ((IHasDomainEvents)session).ClearDomainEvents();

        var newToken = RefreshTokenHash.Create("new-token");
        session.UpdateRefreshToken(newToken, now.AddDays(1));

        session.RefreshTokenHash.Should().Be(newToken);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionRefreshTokenRotatedDomainEvent);
        var evt = (UserSessionRefreshTokenRotatedDomainEvent)session.DomainEvents.Single(e => e is UserSessionRefreshTokenRotatedDomainEvent);
        evt.SessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(session.UserId);
        evt.OccurredAt.Should().Be(now.AddDays(1));
    }

    [Fact]
    public void Session_ShouldExtendAggregateRoot()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);

        session.Should().BeAssignableTo<AggregateRoot>();
    }
}
