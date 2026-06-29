using FluentAssertions;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing.Subscriptions;

public class SubscriptionLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Subscription_ScheduleCancellation_ShouldRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.ClearDomainEvents();
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
    public void Subscription_ScheduleCancellation_WhenAlreadyScheduled_ShouldNotRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.ScheduleCancellation(Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.ScheduleCancellation(Actor, Now);

        sub.Version.Should().Be(version);
        sub.DomainEvents.Should().NotContain(e => e is SubscriptionCancellationScheduledDomainEvent);
    }

    [Fact]
    public void Subscription_SoftDelete_ShouldRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        var version = sub.Version;

        sub.SoftDelete(Actor, Now);

        sub.IsDeleted.Should().BeTrue();
        sub.Version.Should().Be(version + 1);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionSoftDeletedDomainEvent);
        var evt = (SubscriptionSoftDeletedDomainEvent)sub.DomainEvents.Single(e => e is SubscriptionSoftDeletedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.SubscriptionId.Should().Be(sub.Id);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void Subscription_Restore_ShouldRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.SoftDelete(Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.Restore(Actor, Now);

        sub.IsDeleted.Should().BeFalse();
        sub.Version.Should().Be(version + 1);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionRestoredDomainEvent);
        var evt = (SubscriptionRestoredDomainEvent)sub.DomainEvents.Single(e => e is SubscriptionRestoredDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void Subscription_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.SoftDelete(Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.SoftDelete(Actor, Now);

        sub.Version.Should().Be(version);
        sub.DomainEvents.Should().NotContain(e => e is SubscriptionSoftDeletedDomainEvent);
    }

    [Fact]
    public void Subscription_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var sub = Subscription.Create(WsA, Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.ClearDomainEvents();
        var version = sub.Version;

        sub.Restore(Actor, Now);

        sub.Version.Should().Be(version);
        sub.DomainEvents.Should().NotContain(e => e is SubscriptionRestoredDomainEvent);
    }
}
