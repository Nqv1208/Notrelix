using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing;

[CoversAggregate(typeof(Entitlement))]
public class EntitlementTests
{
    private static readonly FeatureCode SampleFeature = FeatureCode.Create("BOARD_COUNT");

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 100, EntitlementSource.Subscription, DateTimeOffset.UtcNow);

        entitlement.Limit.Should().Be(100);
        entitlement.Status.Should().Be(EntitlementStatus.Active);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementGrantedDomainEvent);
    }

    [Fact]
    public void Create_WithNegativeLimit_ShouldThrow()
    {
        var act = () => Entitlement.Create(Guid.NewGuid(), SampleFeature, -1, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "ChangeLimit(System.Int32,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void ChangeLimit_ShouldUpdate_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 100, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.ChangeLimit(200, Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.Limit.Should().Be(200);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementLimitChangedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "ChangeLimit(System.Int32,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void ChangeLimit_WhenSameLimit_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 100, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.ChangeLimit(100, Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "ChangeLimit(System.Int32,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void ChangeLimit_WhenNegative_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        var act = () => entitlement.ChangeLimit(-1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "ChangeLimit(System.Int32,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void ChangeLimit_WhenDisabled_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => entitlement.ChangeLimit(20, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*non-active*");
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "Disable(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Disable_ShouldTransition_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.Status.Should().Be(EntitlementStatus.Disabled);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementDisabledDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "Disable(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void Disable_WhenAlreadyDisabled_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "Disable(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Disable_WhenRevoked_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*revoked*");
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "Revoke(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Revoke_ShouldTransition_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.Status.Should().Be(EntitlementStatus.Revoked);
        entitlement.RevokedAt.Should().NotBeNull();
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementRevokedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "Revoke(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void Revoke_WhenAlreadyRevoked_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "MarkExpired(System.DateTimeOffset)", MutationScenario.Valid)]
    public void MarkExpired_ShouldTransition_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.MarkExpired(DateTimeOffset.UtcNow);

        entitlement.Status.Should().Be(EntitlementStatus.Expired);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementExpiredDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "MarkExpired(System.DateTimeOffset)", MutationScenario.NoOp)]
    public void MarkExpired_WhenAlreadyExpired_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.MarkExpired(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)entitlement).ClearDomainEvents();

        entitlement.MarkExpired(DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Entitlement), "MarkExpired(System.DateTimeOffset)", MutationScenario.Invalid)]
    public void MarkExpired_WhenRevoked_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => entitlement.MarkExpired(DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*revoked*");
    }

    [Fact]
    public void IsActiveAt_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow, expiresAt: DateTimeOffset.UtcNow.AddDays(30));

        entitlement.IsActiveAt(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsActiveAt_WhenExpired_ShouldReturnFalse()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow, expiresAt: DateTimeOffset.UtcNow.AddDays(-1));

        entitlement.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsActiveAt_WhenDisabled_ShouldReturnFalse()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

}
