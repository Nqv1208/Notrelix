using FluentAssertions;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing.Subscriptions;

public class SubscriptionBoundaryTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Subscription_ChangePlan_WhenCanceled_ShouldThrow()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        sub.CancelImmediately(Actor, Now);
        var act = () => sub.ChangePlan(Guid.NewGuid(), Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*inactive*");
    }

    [Fact]
    public void Subscription_ChangePlan_WhenActive_ShouldSucceed()
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        ((IHasDomainEvents)sub).ClearDomainEvents();
        var newPlanId = Guid.NewGuid();
        sub.ChangePlan(newPlanId, Actor, Now);
        sub.PlanId.Should().Be(newPlanId);
        sub.DomainEvents.Should().ContainSingle(e => e is SubscriptionChangedDomainEvent);
    }
}
