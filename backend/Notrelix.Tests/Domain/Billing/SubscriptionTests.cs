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
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionStartedDomainEvent);
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
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionChangedDomainEvent);
    }

    [Fact]
    public void CancelImmediately_ShouldUpdateStatus_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.CancelImmediately(actor, now);

        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionCanceledDomainEvent);
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
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionRenewedDomainEvent);
    }

    [Fact]
    public void Expire_ShouldUpdateStatus_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.Expire(actor, now);

        subscription.Status.Should().Be(SubscriptionStatus.Expired);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionExpiredDomainEvent);
    }

    [Fact]
    public void MarkPastDue_ShouldUpdateStatus_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.MarkPastDue(actor, now);

        subscription.Status.Should().Be(SubscriptionStatus.PastDue);
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionPastDueDomainEvent);
    }

    [Fact]
    public void ScheduleCancellation_ShouldSetFlag_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.ScheduleCancellation(actor, now);

        subscription.CancelAtPeriodEnd.Should().BeTrue();
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionCancellationScheduledDomainEvent);
    }

    [Fact]
    public void ScheduleCancellation_WhenAlreadyScheduled_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.ScheduleCancellation(actor, now);
        subscription.ClearDomainEvents();

        subscription.ScheduleCancellation(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void CancelImmediately_WhenAlreadyCanceled_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.CancelImmediately(actor, now);
        subscription.ClearDomainEvents();

        subscription.CancelImmediately(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Expire_WhenAlreadyExpired_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.Expire(actor, now);
        subscription.ClearDomainEvents();

        subscription.Expire(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void MarkPastDue_WhenAlreadyPastDue_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.MarkPastDue(actor, now);
        subscription.ClearDomainEvents();

        subscription.MarkPastDue(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangePlan_WhenCanceled_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.CancelImmediately(actor, now);

        var act = () => subscription.ChangePlan(Guid.NewGuid(), actor, now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*inactive*");
    }

    [Fact]
    public void ChangePlan_WhenExpired_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.Expire(actor, now);

        var act = () => subscription.ChangePlan(Guid.NewGuid(), actor, now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*inactive*");
    }

    [Fact]
    public void Renew_WithInvalidPeriod_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        var act = () => subscription.Renew(now.AddDays(30), now.AddDays(20), actor, now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*must be before end*");
    }

    [Fact]
    public void Renew_ShouldResetCancelFlag()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.ScheduleCancellation(actor, now);

        subscription.Renew(now.AddDays(30), now.AddDays(60), actor, now);

        subscription.CancelAtPeriodEnd.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_ShouldMarkDeleted_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.ClearDomainEvents();

        subscription.SoftDelete(actor, now);

        subscription.IsDeleted.Should().BeTrue();
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionSoftDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.SoftDelete(actor, now);
        subscription.ClearDomainEvents();

        subscription.SoftDelete(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldRestore_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.SoftDelete(actor, now);
        subscription.ClearDomainEvents();

        subscription.Restore(actor, now);

        subscription.IsDeleted.Should().BeFalse();
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionRestoredDomainEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.ClearDomainEvents();

        subscription.Restore(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ChangePlan_WhenDeleted_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.SoftDelete(actor, now);

        var act = () => subscription.ChangePlan(Guid.NewGuid(), actor, now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Renew_WhenDeleted_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.SoftDelete(actor, now);

        var act = () => subscription.Renew(now.AddDays(30), now.AddDays(60), actor, now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }
}
