using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing;

[CoversAggregate(typeof(Subscription))]
public class SubscriptionTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();

        var subscription = Subscription.Create(Guid.NewGuid(), planId, SubscriptionTier.Pro, now, now.AddDays(30), actor, now, workspaceId);

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

        var act = () => Subscription.Create(Guid.NewGuid(), planId, SubscriptionTier.Pro, now, now.AddMinutes(-5), Guid.NewGuid(), now);
        act.Should().Throw<BusinessRuleException>().WithMessage("Subscription period start must be before end.");
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.ChangePlan), MutationScenario.Valid, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.CancelImmediately), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    public void CancelImmediately_ShouldUpdateStatus_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        subscription.CancelImmediately(actor, now);

        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
        subscription.CancelAtPeriodEnd.Should().BeFalse();
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionCanceledDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.Renew), MutationScenario.Valid, typeof(DateTimeOffset), typeof(DateTimeOffset), typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.Expire), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.MarkPastDue), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.ScheduleCancellation), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.ScheduleCancellation), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    public void ScheduleCancellation_WhenAlreadyScheduled_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.ScheduleCancellation(actor, now);
        ((IHasDomainEvents)subscription).ClearDomainEvents();

        subscription.ScheduleCancellation(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.CancelImmediately), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    public void CancelImmediately_WhenAlreadyCanceled_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.CancelImmediately(actor, now);
        ((IHasDomainEvents)subscription).ClearDomainEvents();

        subscription.CancelImmediately(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.Expire), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    public void Expire_WhenAlreadyExpired_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.Expire(actor, now);
        ((IHasDomainEvents)subscription).ClearDomainEvents();

        subscription.Expire(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.MarkPastDue), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    public void MarkPastDue_WhenAlreadyPastDue_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.MarkPastDue(actor, now);
        ((IHasDomainEvents)subscription).ClearDomainEvents();

        subscription.MarkPastDue(actor, now);

        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.ChangePlan), MutationScenario.Invalid, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.ChangePlan), MutationScenario.Invalid, typeof(Guid), typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.Renew), MutationScenario.Invalid, typeof(DateTimeOffset), typeof(DateTimeOffset), typeof(Guid), typeof(DateTimeOffset))]
    public void Renew_WithInvalidPeriod_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);

        var act = () => subscription.Renew(now.AddDays(30), now.AddDays(20), actor, now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*must be before end*");
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.Renew), MutationScenario.Valid, typeof(DateTimeOffset), typeof(DateTimeOffset), typeof(Guid), typeof(DateTimeOffset))]
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
    [CoversMutation(typeof(Subscription), nameof(Subscription.CancelImmediately), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    public void CancelImmediately_ScheduledCancellation_ShouldClearFlag()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var subscription = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, now, now.AddDays(30), actor, now);
        subscription.ScheduleCancellation(actor, now);
        subscription.CancelAtPeriodEnd.Should().BeTrue();

        subscription.CancelImmediately(actor, now);

        subscription.CancelAtPeriodEnd.Should().BeFalse();
        subscription.Status.Should().Be(SubscriptionStatus.Canceled);
    }
}
