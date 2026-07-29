using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing.Subscriptions;

public class SubscriptionCancelImmediatelyTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    [CoversMutation(typeof(Subscription), "CancelImmediately(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void CancelImmediately_SetsStatusToCanceled()
    {
        var sub = Subscription.Create(Guid.NewGuid(), WsA, SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);

        sub.CancelImmediately(Actor, Now);

        sub.Status.Should().Be(SubscriptionStatus.Canceled);
        sub.CancelAtPeriodEnd.Should().BeFalse();
    }

    [Fact]
    [CoversMutation(typeof(Subscription), "CancelImmediately(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void CancelImmediately_WhenAlreadyCanceled_ShouldBeNoOp()
    {
        var sub = Subscription.Create(Guid.NewGuid(), WsA, SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);
        sub.CancelImmediately(Actor, Now);
        var eventsBefore = sub.DomainEvents.Count();

        sub.CancelImmediately(Actor, Now.AddDays(2));

        sub.Status.Should().Be(SubscriptionStatus.Canceled);
        sub.DomainEvents.Count().Should().Be(eventsBefore);
    }

    [Fact]
    [CoversMutation(typeof(Subscription), "CancelImmediately(System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void CancelImmediately_WhenExpired_ShouldThrow()
    {
        var sub = Subscription.Create(Guid.NewGuid(), WsA, SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);
        sub.Expire(Actor, Now.AddDays(60));

        var act = () => sub.CancelImmediately(Actor, Now.AddDays(61));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    [CoversMutation(typeof(Subscription), "CancelImmediately(System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void CancelImmediately_ClearsCancelAtPeriodEnd()
    {
        var sub = Subscription.Create(Guid.NewGuid(), WsA, SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, WsA);
        sub.ScheduleCancellation(Actor, Now.AddHours(1));
        sub.CancelAtPeriodEnd.Should().BeTrue();

        sub.CancelImmediately(Actor, Now.AddDays(2));

        sub.CancelAtPeriodEnd.Should().BeFalse();
    }
}
