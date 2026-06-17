using FluentAssertions;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Entitlements.Events;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Billing;

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
    public void ChangeLimit_ShouldUpdate_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 100, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.ChangeLimit(200, Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.Limit.Should().Be(200);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementLimitChangedDomainEvent);
    }

    [Fact]
    public void ChangeLimit_WhenSameLimit_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 100, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.ChangeLimit(100, Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangeLimit_WhenNegative_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        var act = () => entitlement.ChangeLimit(-1, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void ChangeLimit_WhenDisabled_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => entitlement.ChangeLimit(20, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*non-active*");
    }

    [Fact]
    public void ChangeLimit_WhenDeleted_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => entitlement.ChangeLimit(20, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Disable_ShouldTransition_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.Status.Should().Be(EntitlementStatus.Disabled);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementDisabledDomainEvent);
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Disable_WhenRevoked_ShouldThrow()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*revoked*");
    }

    [Fact]
    public void Revoke_ShouldTransition_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.Status.Should().Be(EntitlementStatus.Revoked);
        entitlement.RevokedAt.Should().NotBeNull();
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementRevokedDomainEvent);
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.Revoke(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkExpired_ShouldTransition_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.MarkExpired(DateTimeOffset.UtcNow);

        entitlement.Status.Should().Be(EntitlementStatus.Expired);
        entitlement.DomainEvents.Should().ContainSingle(e => e is EntitlementExpiredDomainEvent);
    }

    [Fact]
    public void MarkExpired_WhenAlreadyExpired_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.MarkExpired(DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.MarkExpired(DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
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
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

        entitlement.IsActiveAt(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsActiveAt_WhenExpired_ShouldReturnFalse()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(-1));

        entitlement.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsActiveAt_WhenDeleted_ShouldReturnFalse()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsActiveAt_WhenDisabled_ShouldReturnFalse()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.IsActiveAt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_ShouldMarkDeleted_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.IsDeleted.Should().BeTrue();
        entitlement.DomainEvents.Should().Contain(e => e is EntitlementSoftDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldRestore_AndRaiseEvent()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.IsDeleted.Should().BeFalse();
        entitlement.DomainEvents.Should().Contain(e => e is EntitlementRestoredEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var entitlement = Entitlement.Create(Guid.NewGuid(), SampleFeature, 10, EntitlementSource.Subscription, DateTimeOffset.UtcNow);
        entitlement.ClearDomainEvents();

        entitlement.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        entitlement.DomainEvents.Should().BeEmpty();
    }
}
