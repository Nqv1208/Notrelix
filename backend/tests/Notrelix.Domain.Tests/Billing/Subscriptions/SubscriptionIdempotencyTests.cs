using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing;

public class SubscriptionIdempotencyTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    [CoversMutation(typeof(Subscription), "CancelImmediately(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void CancelImmediately_ShouldNotIncrementVersion_WhenAlreadyCanceled()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.CancelImmediately(_actorId, _now);
        var version = sub.Version;

        sub.CancelImmediately(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    [Fact]
    [CoversMutation(typeof(Subscription), "ScheduleCancellation(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void ScheduleCancellation_ShouldNotIncrementVersion_WhenAlreadyScheduled()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.ScheduleCancellation(_actorId, _now);
        var version = sub.Version;

        sub.ScheduleCancellation(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    [Fact]
    [CoversMutation(typeof(Subscription), "Expire(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void Expire_ShouldNotIncrementVersion_WhenAlreadyExpired()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.Expire(_actorId, _now);
        var version = sub.Version;

        sub.Expire(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    [Fact]
    [CoversMutation(typeof(Subscription), "MarkPastDue(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void MarkPastDue_ShouldNotIncrementVersion_WhenAlreadyPastDue()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, _now, _now.AddDays(30), _actorId, _now);
        sub.MarkPastDue(_actorId, _now);
        var version = sub.Version;

        sub.MarkPastDue(_actorId, _now);

        sub.Version.Should().Be(version);
    }

    [Fact]
    public void DomainEvent_ShouldCarryNullActor_WhenCreatedBySystem()
    {
        var subscription = Subscription.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubscriptionTier.Pro,
            _now,
            _now.AddDays(30),
            null,
            _now);

        subscription.DomainEvents.Single(e => e is SubscriptionStartedDomainEvent).Should().NotBeNull();
    }
}
