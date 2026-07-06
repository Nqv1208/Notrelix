using FluentAssertions;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing.Entitlements;

public class EntitlementLifecycleTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Entitlement_Create_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now);

        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementGrantedDomainEvent);
        var evt = (EntitlementGrantedDomainEvent)entitlement.DomainEvents.Single(e => e is EntitlementGrantedDomainEvent);
        evt.EntitlementId.Should().Be(entitlement.Id);
        evt.FeatureCode.Should().Be("BOARDS");
        evt.Limit.Should().Be(10);
    }

    [Fact]
    public void Entitlement_SoftDelete_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.SoftDelete(Actor, Now);

        entitlement.IsDeleted.Should().BeTrue();
        entitlement.Version.Should().Be(version + 1);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementSoftDeletedDomainEvent);
        var evt = (EntitlementSoftDeletedDomainEvent)entitlement.DomainEvents.Single(e => e is EntitlementSoftDeletedDomainEvent);
        evt.AccountId.Should().Be(AccountId);
        evt.EntitlementId.Should().Be(entitlement.Id);
        evt.FeatureCode.Should().Be("BOARDS");
    }

    [Fact]
    public void Entitlement_Restore_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.SoftDelete(Actor, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.Restore(Actor, Now);

        entitlement.IsDeleted.Should().BeFalse();
        entitlement.Version.Should().Be(version + 1);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementRestoredDomainEvent);
        var evt = (EntitlementRestoredDomainEvent)entitlement.DomainEvents.Single(e => e is EntitlementRestoredDomainEvent);
        evt.AccountId.Should().Be(AccountId);
        evt.EntitlementId.Should().Be(entitlement.Id);
    }

    [Fact]
    public void Entitlement_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.SoftDelete(Actor, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.SoftDelete(Actor, Now);

        entitlement.Version.Should().Be(version);
        entitlement.DomainEvents.Should().NotContain(e => e is EntitlementSoftDeletedDomainEvent);
    }

    [Fact]
    public void Entitlement_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now);
        entitlement.ClearDomainEvents();
        var version = entitlement.Version;

        entitlement.Restore(Actor, Now);

        entitlement.Version.Should().Be(version);
        entitlement.DomainEvents.Should().NotContain(e => e is EntitlementRestoredDomainEvent);
    }
}
