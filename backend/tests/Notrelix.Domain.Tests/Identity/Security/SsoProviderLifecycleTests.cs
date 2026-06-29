using FluentAssertions;

namespace Notrelix.Domain.Tests.Identity;

public class SsoProviderLifecycleTests
{
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_ShouldRaiseCreatedEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);

        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderCreatedDomainEvent);
        var evt = (SsoProviderCreatedDomainEvent)provider.DomainEvents.Single(e => e is SsoProviderCreatedDomainEvent);
        evt.ProviderId.Should().Be(provider.Id);
        evt.Name.Should().Be("My IdP");
    }

    [Fact]
    public void Disable_ShouldRaiseDisabledEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);
        provider.ClearDomainEvents();
        var version = provider.Version;

        provider.Disable(_actorId, _now);

        provider.Version.Should().Be(version + 1);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderDisabledDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);
        provider.ClearDomainEvents();
        var version = provider.Version;

        provider.SoftDelete(_actorId, _now);

        provider.IsDeleted.Should().BeTrue();
        provider.Version.Should().Be(version + 1);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderSoftDeletedDomainEvent);
    }

    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var provider = SsoProvider.Create(workspaceId, SsoProviderType.Oidc, "My IdP", _actorId, _now);
        provider.SoftDelete(_actorId, _now);
        provider.ClearDomainEvents();
        var version = provider.Version;

        provider.Restore(_actorId, _now);

        provider.IsDeleted.Should().BeFalse();
        provider.Version.Should().Be(version + 1);
        provider.DomainEvents.Should().ContainSingle(e => e is SsoProviderRestoredDomainEvent);
    }
}
