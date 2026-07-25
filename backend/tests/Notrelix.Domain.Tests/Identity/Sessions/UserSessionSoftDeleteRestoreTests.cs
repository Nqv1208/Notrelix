using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserSessionSoftDeleteRestoreTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void UpdateRefreshToken_ShouldIncrementVersion_AndRaiseEvent()
    {
        var tokenHash = RefreshTokenHash.Create("refresh-token");
        var session = UserSession.Create(_actorId, tokenHash, _now.AddDays(30), _now);
        ((IHasDomainEvents)session).ClearDomainEvents();
        var version = session.Version;

        var newHash = RefreshTokenHash.Create("new-refresh-token");
        session.UpdateRefreshToken(newHash, _now);

        session.Version.Should().Be(version + 1);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionRefreshTokenRotatedDomainEvent);
        var evt = (UserSessionRefreshTokenRotatedDomainEvent)session.DomainEvents.Single(e => e is UserSessionRefreshTokenRotatedDomainEvent);
        evt.SessionId.Should().Be(session.Id);
        evt.UserId.Should().Be(session.UserId);
    }

    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var tokenHash = RefreshTokenHash.Create("refresh-token");
        var session = UserSession.Create(_actorId, tokenHash, _now.AddDays(30), _now);
        var version = session.Version;

        session.SoftDelete(_actorId, _now);

        session.IsDeleted.Should().BeTrue();
        session.Version.Should().Be(version + 1);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var tokenHash = RefreshTokenHash.Create("refresh-token");
        var session = UserSession.Create(_actorId, tokenHash, _now.AddDays(30), _now);
        session.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)session).ClearDomainEvents();
        var version = session.Version;

        session.Restore(_actorId, _now);

        session.IsDeleted.Should().BeFalse();
        session.Version.Should().Be(version + 1);
        session.DomainEvents.Should().ContainSingle(e => e is UserSessionRestoredDomainEvent);
    }
}
