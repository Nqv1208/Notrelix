using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity;

public class UserSoftDeleteRestoreTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(User), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        var version = user.Version;

        user.SoftDelete(_actorId, _now);

        user.IsDeleted.Should().BeTrue();
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserSoftDeletedDomainEvent);
        var evt = (UserSoftDeletedDomainEvent)user.DomainEvents.Single(e => e is UserSoftDeletedDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.DeletedBy.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now);
    }

    [CoversMutation(typeof(User), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        user.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.Restore(_actorId, _now);

        user.IsDeleted.Should().BeFalse();
        user.Version.Should().Be(version + 1);
        user.DomainEvents.Should().ContainSingle(e => e is UserRestoredDomainEvent);
        var evt = (UserRestoredDomainEvent)user.DomainEvents.Single(e => e is UserRestoredDomainEvent);
        evt.UserId.Should().Be(user.Id);
        evt.RestoredBy.Should().Be(_actorId);
    }

    [CoversMutation(typeof(User), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    [Fact]
    public void SoftDelete_ShouldNotIncrementOrRaiseEvent_WhenAlreadyDeleted()
    {
        var user = User.Create("test@example.com", "Test", "hash", _now);
        user.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)user).ClearDomainEvents();
        var version = user.Version;

        user.SoftDelete(_actorId, _now);

        user.Version.Should().Be(version);
        user.DomainEvents.Should().NotContain(e => e is UserSoftDeletedDomainEvent);
    }

    [CoversMutation(typeof(User), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
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
