using FluentAssertions;

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

    [Fact]
    public void Revoke_WithReason_PropagatesStableReasonIntoEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        ((IHasDomainEvents)session).ClearDomainEvents();

        session.Revoke(now.AddDays(1), SessionRevocationReasons.PasswordChanged);

        var evt = (UserSessionRevokedDomainEvent)session.DomainEvents.First(e => e is UserSessionRevokedDomainEvent);
        evt.Reason.Should().Be(SessionRevocationReasons.PasswordChanged);
    }

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

    [Fact]
    public void Revoke_OnExpiredSession_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Expire(now.AddDays(1));

        var act = () => session.Revoke(now.AddDays(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*expired session*");
    }

    [Fact]
    public void Expire_OnRevokedSession_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Revoke(now.AddDays(1));

        var act = () => session.Expire(now.AddDays(2));

        act.Should().Throw<BusinessRuleException>().WithMessage("*revoked session*");
    }

    [Fact]
    public void Rotate_ShouldRotateTokenAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        ((IHasDomainEvents)session).ClearDomainEvents();

        var newToken = RefreshTokenHash.Create("new-token");
        session.Rotate(newToken, now.AddDays(1));

        session.RefreshTokenHash.Should().Be(newToken);
        session.PreviousRefreshTokenHash.Should().Be(ValidTokenHash.Hash);
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

    [Fact]
    public void Rotate_KeepsTheSessionAndRemembersTheReplacedCredentialForTheGraceWindowOnly()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        var next = RefreshTokenHash.Create("next-refresh-token");

        session.Rotate(next, now.AddMinutes(5));

        session.Status.Should().Be(SessionStatus.Active);
        session.RefreshTokenHash.Hash.Should().Be(next.Hash);
        var rotatedAt = now.AddMinutes(5);
        // A retry of the lost response inside the window is not a replay.
        session.IsWithinRotationGrace(ValidTokenHash.Hash, rotatedAt.AddSeconds(10)).Should().BeTrue();
        // After the window the same credential is a replay.
        session.IsWithinRotationGrace(ValidTokenHash.Hash, rotatedAt.AddSeconds(11)).Should().BeFalse();
        // Only the credential the last rotation replaced is recognised.
        session.IsWithinRotationGrace(RefreshTokenHash.Create("never-issued").Hash, rotatedAt).Should().BeFalse();
    }

    [Fact]
    public void Rotate_SetsTheIdleExpiryFromNow_AndNeverPastTheAbsoluteLimit()
    {
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, created.AddDays(30), created);

        var early = created.AddDays(10);
        session.Rotate(RefreshTokenHash.Create("a"), early);
        session.ExpiresAt.Should().Be(early + SessionLifetimePolicy.IdleLifetime);

        // Used daily for almost the whole absolute life: the cap wins over the idle period.
        var late = created.AddDays(85);
        session.Rotate(RefreshTokenHash.Create("b"), late);
        session.ExpiresAt.Should().Be(created + SessionLifetimePolicy.AbsoluteLifetime);
    }

    [Fact]
    public void Rotate_AfterTheAbsoluteLimit_IsRefusedAndChangesNothing()
    {
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, created.AddDays(30), created);
        var version = session.Version;

        var act = () => session.Rotate(RefreshTokenHash.Create("late"), created.AddDays(91));

        act.Should().Throw<BusinessRuleException>();
        session.RefreshTokenHash.Hash.Should().Be(ValidTokenHash.Hash);
        session.Version.Should().Be(version);
    }

    [Fact]
    public void Rotate_OfARevokedSession_IsRefused()
    {
        var now = DateTimeOffset.UtcNow;
        var session = UserSession.Create(Guid.NewGuid(), ValidTokenHash, now.AddDays(30), now);
        session.Revoke(now.AddMinutes(1));

        var act = () => session.Rotate(RefreshTokenHash.Create("x"), now.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>();
    }
}
