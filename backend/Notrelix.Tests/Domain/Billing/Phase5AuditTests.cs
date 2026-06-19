using FluentAssertions;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Tests.Billing;

public class Phase5AuditTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Entitlement_Create_WithNegativeLimit_ShouldThrow()
    {
        var feature = FeatureCode.Create("boards");
        var act = () => Entitlement.Create(WsA, feature, -1, EntitlementSource.Subscription, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Entitlement_Create_WithZeroLimit_ShouldSucceed()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(WsA, feature, 0, EntitlementSource.Subscription, Now);
        entitlement.Limit.Should().Be(0);
    }

    [Fact]
    public void Usage_Consume_WithZeroAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 0, 100, null, Now);
        var act = () => usage.Consume(0, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Release_WithZeroAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 50, 100, null, Now);
        var act = () => usage.Release(0, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Consume_WithNegativeAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 0, 100, null, Now);
        var act = () => usage.Consume(-5, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Release_WithNegativeAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 50, 100, null, Now);
        var act = () => usage.Release(-5, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Create_WithNegativeCurrentUsage_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), -1, 100, null, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Usage_Create_WithNegativeHardLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 0, -1, null, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Usage_Create_WithNegativeSoftLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 0, 100, -1, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Usage_Create_WithSoftExceedingHard_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 0, 100, 150, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*soft limit*");
    }

    [Fact]
    public void Usage_Create_WithUsageExceedingHardLimit_WhenOverageDisallowed_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 150, 100, null, Now, overageAllowed: false);
        act.Should().Throw<BusinessRuleException>().WithMessage("*overage*");
    }

    [Fact]
    public void Usage_Create_WithUsageExceedingHardLimit_WhenOverageAllowed_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(WsA, FeatureCode.Create("storage"), 150, 100, null, Now, overageAllowed: true);
        usage.CurrentUsage.Should().Be(150);
    }

    [Fact]
    public void Subscription_ChangePlan_WhenCanceled_ShouldThrow()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.CancelImmediately(Actor, Now);
        var act = () => sub.ChangePlan(Guid.NewGuid(), Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*inactive*");
    }

    [Fact]
    public void Subscription_ChangePlan_WhenActive_ShouldSucceed()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.ClearDomainEvents();
        var newPlanId = Guid.NewGuid();
        sub.ChangePlan(newPlanId, Actor, Now);
        sub.PlanId.Should().Be(newPlanId);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionChangedDomainEvent);
    }
}
