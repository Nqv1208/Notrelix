using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity;

public class UserSessionSoftDeleteRestoreTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(UserSession), "UpdateRefreshToken(Notrelix.Domain.Identity.Sessions.RefreshTokenHash,System.DateTimeOffset)", MutationScenario.Version)]
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

    [CoversMutation(typeof(UserSession), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
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

    [CoversMutation(typeof(UserSession), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
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
