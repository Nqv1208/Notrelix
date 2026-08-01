using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class UserDeleteRestoreTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Delete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        var version = user.Version;

        user.Delete(_actorId, _now);

        user.IsDeleted.Should().BeTrue();
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserDeletedDomainEvent);
        var evt = (UserDeletedDomainEvent)user.DomainEvents.Single(e => e is UserDeletedDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Status.Should().Be(user.Status);
        evt.DeletedBy.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        user.Delete(_actorId, _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.Restore(_actorId, _now);

        user.IsDeleted.Should().BeFalse();
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserRestoredDomainEvent);
        var evt = (UserRestoredDomainEvent)user.DomainEvents.Single(e => e is UserRestoredDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.Status.Should().Be(user.Status);
        evt.RestoredBy.Should().Be(_actorId);
    }

    [Fact]
    public void Delete_ShouldNotIncrementOrRaiseEvent_WhenAlreadyDeleted()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        user.Delete(_actorId, _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.Delete(_actorId, _now);

        user.Version.Should().Be(version);
        user.DomainEvents.Should().NotContain(e => e is UserDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldNotIncrementOrRaiseEvent_WhenNotDeleted()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.Restore(_actorId, _now);

        user.Version.Should().Be(version);
        user.DomainEvents.Should().NotContain(e => e is UserRestoredDomainEvent);
    }
}
