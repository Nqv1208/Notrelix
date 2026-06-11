using FluentAssertions;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Billing;

public class SubscriptionTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();

        var subscription = Subscription.Create(workspaceId, planId, SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.WorkspaceId.Should().Be(workspaceId);
        subscription.PlanId.Should().Be(planId);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.Tier.Should().Be(SubscriptionTier.Pro);
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionStartedEvent);
    }

    [Fact]
    public void Create_WithInvalidPeriod_ShouldThrowBusinessRuleException()
    {
        var workspaceId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var act = () => Subscription.Create(workspaceId, planId, SubscriptionTier.Pro, now, now.AddMinutes(-5), Guid.NewGuid(), now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Subscription period start must be before end.");
    }

    [Fact]
    public void ChangePlan_ShouldUpdatePlanId_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        var newPlanId = Guid.NewGuid();
        subscription.ChangePlan(newPlanId, actor, now);

        subscription.PlanId.Should().Be(newPlanId);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionChangedEvent);
    }

    [Fact]
    public void CancelImmediately_ShouldUpdateStatus_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.CancelImmediately(actor, now);

        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionCancelledEvent);
    }

    [Fact]
    public void Renew_ShouldUpdatePeriod_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        var nextStart = now.AddDays(30);
        var nextEnd = now.AddDays(60);
        subscription.Renew(nextStart, nextEnd, actor, now);

        subscription.CurrentPeriodStart.Should().Be(nextStart);
        subscription.CurrentPeriodEnd.Should().Be(nextEnd);
        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.CancelAtPeriodEnd.Should().BeFalse();
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionRenewedEvent);
    }

    [Fact]
    public void Expire_ShouldUpdateStatus_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.Expire(actor, now);

        subscription.Status.Should().Be(SubscriptionStatus.Expired);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionExpiredEvent);
    }

    [Fact]
    public void MarkPastDue_ShouldUpdateStatus_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.MarkPastDue(actor, now);

        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionPastDueEvent);
    }
}
