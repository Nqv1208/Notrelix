using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity;

public class UserSecuritySettingsLifecycleTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldRaiseCreatedEvent()
    {
        var settings = UserSecuritySettings.Create(_actorId, _now);

        settings.DomainEvents.Should().ContainSingle(e => e is UserSecuritySettingsCreatedDomainEvent);
        var evt = (UserSecuritySettingsCreatedDomainEvent)settings.DomainEvents.Single(e => e is UserSecuritySettingsCreatedDomainEvent);
        evt.UserSecuritySettingsId.Should().Be(settings.Id);
        evt.UserId.Should().Be(_actorId);
        evt.OccurredAt.Should().Be(_now);
    }

    [CoversMutation(typeof(UserSecuritySettings), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var settings = UserSecuritySettings.Create(_actorId, _now);
        ((IHasDomainEvents)settings).ClearDomainEvents();
        var version = settings.Version;

        settings.SoftDelete(_actorId, _now);

        settings.IsDeleted.Should().BeTrue();
        settings.Version.Should().Be(version + 1);
        settings.DomainEvents.Should().ContainSingle(e => e is UserSecuritySettingsSoftDeletedDomainEvent);
    }

    [CoversMutation(typeof(UserSecuritySettings), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var settings = UserSecuritySettings.Create(_actorId, _now);
        settings.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)settings).ClearDomainEvents();
        var version = settings.Version;

        settings.Restore(_actorId, _now);

        settings.IsDeleted.Should().BeFalse();
        settings.Version.Should().Be(version + 1);
        settings.DomainEvents.Should().ContainSingle(e => e is UserSecuritySettingsRestoredDomainEvent);
    }
}
