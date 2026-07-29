using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Identity;

public class UserMfaMethodSoftDeleteRestoreTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(UserMfaMethod), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(UserMfaMethod), "Disable(System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var secretRef = SecretRef.Create("secret-123");
        var method = UserMfaMethod.Create(_actorId, MfaMethodType.AuthenticatorApp, _now, secretRef);
        var version = method.Version;

        method.SoftDelete(_actorId, _now);

        method.IsDeleted.Should().BeTrue();
        method.Version.Should().Be(version + 1);
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodSoftDeletedDomainEvent);
    }

    [CoversMutation(typeof(UserMfaMethod), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var secretRef = SecretRef.Create("secret-123");
        var method = UserMfaMethod.Create(_actorId, MfaMethodType.AuthenticatorApp, _now, secretRef);
        method.SoftDelete(_actorId, _now);
        ((IHasDomainEvents)method).ClearDomainEvents();
        var version = method.Version;

        method.Restore(_actorId, _now);

        method.IsDeleted.Should().BeFalse();
        method.Version.Should().Be(version + 1);
        method.DomainEvents.Should().ContainSingle(e => e is UserMfaMethodRestoredDomainEvent);
    }
}
