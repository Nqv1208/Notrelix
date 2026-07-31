using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing.Subscriptions;

public class SubscriptionLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.ScheduleCancellation), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [CoversMutation(typeof(Subscription), nameof(Subscription.ScheduleCancellation), MutationScenario.Version, typeof(Guid), typeof(DateTimeOffset))]
    public void Subscription_ScheduleCancellation_ShouldRaiseEvent()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);
        ((IHasDomainEvents)sub).ClearDomainEvents();
        var version = sub.Version;

        sub.ScheduleCancellation(Actor, Now);

        sub.Version.Should().Be(version + 1);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionCancellationScheduledDomainEvent);
        var evt = (SubscriptionCancellationScheduledDomainEvent)sub.DomainEvents.Single(e => e is SubscriptionCancellationScheduledDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.SubscriptionId.Should().Be(sub.Id);
        evt.UpdatedBy.Should().Be(Actor);
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.ScheduleCancellation), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    public void Subscription_ScheduleCancellation_WhenAlreadyScheduled_ShouldNotRaiseEvent()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);
        sub.ScheduleCancellation(Actor, Now);
        ((IHasDomainEvents)sub).ClearDomainEvents();
        var version = sub.Version;

        sub.ScheduleCancellation(Actor, Now);

        sub.Version.Should().Be(version);
        sub.DomainEvents.Should().NotContain(e => e is SubscriptionCancellationScheduledDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(Subscription), nameof(Subscription.CancelImmediately), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    public void Subscription_CancelImmediately_ShouldClearCancelAtPeriodEnd()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);
        sub.ScheduleCancellation(Actor, Now);
        sub.CancelAtPeriodEnd.Should().BeTrue();
        ((IHasDomainEvents)sub).ClearDomainEvents();

        sub.CancelImmediately(Actor, Now);

        sub.CancelAtPeriodEnd.Should().BeFalse();
        sub.Status.Should().Be(SubscriptionStatus.Canceled);
    }

    [Fact]
    public void Subscription_IsNotDeletedAggregate()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);

        sub.Should().NotBeAssignableTo<SoftDeletableAggregateRoot>();
    }
}
