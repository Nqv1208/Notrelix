using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity.Profiles;

public class UserProfileDeleteRestoreTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Delete_ShouldMarkDeleted()
    {
        var profile = UserProfile.Create(_actorId, _now);

        profile.Delete(_actorId, _now.AddMinutes(1));

        profile.IsDeleted.Should().BeTrue();
        profile.DeletedAt.Should().Be(_now.AddMinutes(1));
        profile.DeletedBy.Should().Be(_actorId);
    }

    [Fact]
    public void Delete_ShouldRaiseEvent()
    {
        var profile = UserProfile.Create(_actorId, _now);
        ((IHasDomainEvents)profile).ClearDomainEvents();

        profile.Delete(_actorId, _now.AddMinutes(1));

        profile.DomainEvents.Should().ContainSingle(e => e is UserProfileDeletedDomainEvent);
        var evt = (UserProfileDeletedDomainEvent)profile.DomainEvents.Single(e => e is UserProfileDeletedDomainEvent);
        evt.UserProfileId.Should().Be(profile.Id);
        evt.UserId.Should().Be(_actorId);
        evt.DeletedBy.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now.AddMinutes(1));
    }

    [Fact]
    public void Delete_AlreadyDeleted_ShouldBeNoOp()
    {
        var profile = UserProfile.Create(_actorId, _now);
        profile.Delete(_actorId, _now.AddMinutes(1));
        ((IHasDomainEvents)profile).ClearDomainEvents();

        profile.Delete(_actorId, _now.AddMinutes(2));

        profile.DomainEvents.Should().BeEmpty();
        profile.Version.Should().Be(2);
    }

    [Fact]
    public void Delete_ShouldIncrementVersion()
    {
        var profile = UserProfile.Create(_actorId, _now);
        var versionBefore = profile.Version;

        profile.Delete(_actorId, _now.AddMinutes(1));

        profile.Version.Should().Be(versionBefore + 1);
    }

    [Fact]
    public void Restore_AfterDelete_ShouldRestore()
    {
        var profile = UserProfile.Create(_actorId, _now);
        profile.Delete(_actorId, _now.AddMinutes(1));
        ((IHasDomainEvents)profile).ClearDomainEvents();

        profile.Restore(_actorId, _now.AddMinutes(2));

        profile.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_ShouldRaiseEvent()
    {
        var profile = UserProfile.Create(_actorId, _now);
        profile.Delete(_actorId, _now.AddMinutes(1));
        ((IHasDomainEvents)profile).ClearDomainEvents();

        profile.Restore(_actorId, _now.AddMinutes(2));

        profile.DomainEvents.Should().ContainSingle(e => e is UserProfileRestoredDomainEvent);
        var evt = (UserProfileRestoredDomainEvent)profile.DomainEvents.Single(e => e is UserProfileRestoredDomainEvent);
        evt.UserProfileId.Should().Be(profile.Id);
        evt.UserId.Should().Be(_actorId);
        evt.RestoredBy.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now.AddMinutes(2));
    }

    [Fact]
    public void Restore_NotDeleted_ShouldBeNoOp()
    {
        var profile = UserProfile.Create(_actorId, _now);
        ((IHasDomainEvents)profile).ClearDomainEvents();

        profile.Restore(_actorId, _now.AddMinutes(1));

        profile.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldIncrementVersion()
    {
        var profile = UserProfile.Create(_actorId, _now);
        profile.Delete(_actorId, _now.AddMinutes(1));
        var versionBefore = profile.Version;

        profile.Restore(_actorId, _now.AddMinutes(2));

        profile.Version.Should().Be(versionBefore + 1);
    }
}
