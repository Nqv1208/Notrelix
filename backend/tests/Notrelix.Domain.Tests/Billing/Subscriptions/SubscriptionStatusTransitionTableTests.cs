using System.Reflection;
using FluentAssertions;
using Notrelix.Domain.Billing;
using Notrelix.Domain.Billing.Rules;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing.Subscriptions;

/// <summary>
/// Every (from, to) pair of <see cref="SubscriptionStatus"/> is checked against a literal copy of the
/// closed table (product BIL-008, section 22), both in the table and through the aggregate methods.
/// </summary>
public class SubscriptionStatusTransitionTableTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static readonly SubscriptionStatus[] Targets =
    {
        SubscriptionStatus.Active,
        SubscriptionStatus.PastDue,
        SubscriptionStatus.Canceled,
        SubscriptionStatus.Expired,
    };

    // Literal expectation of the table: the sources that may move to each target. Staying put is allowed too.
    private static readonly IReadOnlyDictionary<SubscriptionStatus, SubscriptionStatus[]> Expected =
        new Dictionary<SubscriptionStatus, SubscriptionStatus[]>
        {
            [SubscriptionStatus.Trialing] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled },
            [SubscriptionStatus.Active] = new[] { SubscriptionStatus.PastDue, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.PastDue] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.Unpaid] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.Incomplete] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Canceled, SubscriptionStatus.Expired },
            [SubscriptionStatus.Canceled] = Array.Empty<SubscriptionStatus>(),
            [SubscriptionStatus.Expired] = Array.Empty<SubscriptionStatus>(),
        };

    public static TheoryData<SubscriptionStatus, SubscriptionStatus> AllPairs()
    {
        var data = new TheoryData<SubscriptionStatus, SubscriptionStatus>();
        foreach (var from in Enum.GetValues<SubscriptionStatus>())
            foreach (var to in Enum.GetValues<SubscriptionStatus>())
                data.Add(from, to);
        return data;
    }

    public static TheoryData<SubscriptionStatus, SubscriptionStatus> TargetPairs()
    {
        var data = new TheoryData<SubscriptionStatus, SubscriptionStatus>();
        foreach (var from in Enum.GetValues<SubscriptionStatus>())
            foreach (var to in Targets)
                data.Add(from, to);
        return data;
    }

    [Fact]
    public void Table_CoversEveryStatus()
    {
        Enum.GetValues<SubscriptionStatus>().Should().BeEquivalentTo(Expected.Keys);
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void CanTransition_MatchesClosedTable(SubscriptionStatus from, SubscriptionStatus to)
    {
        var expected = from == to || Expected[from].Contains(to);

        SubscriptionStatusTransitions.CanTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [MemberData(nameof(TargetPairs))]
    public void AggregateMethods_AllowExactlyTheTable(SubscriptionStatus from, SubscriptionStatus to)
    {
        var expectedAllowed = from == to || Expected[from].Contains(to);
        var sub = SubscriptionIn(from);
        var version = sub.Version;

        var act = () => Move(sub, to);

        if (expectedAllowed)
        {
            act.Should().NotThrow();
            sub.Status.Should().Be(to);
        }
        else
        {
            act.Should().Throw<BusinessRuleException>()
                .Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Subscription_InvalidStatusTransition);
            sub.Status.Should().Be(from);
            sub.Version.Should().Be(version);
            sub.DomainEvents.Should().BeEmpty();
        }
    }

    [Fact]
    public void ScheduleCancellation_IsOnlyOpenForActiveAndPastDue()
    {
        foreach (var status in Enum.GetValues<SubscriptionStatus>())
        {
            var sub = SubscriptionIn(status);
            var act = () => sub.ScheduleCancellation(Actor, Now);

            if (SubscriptionStatusTransitions.CanScheduleCancellation(status))
            {
                act.Should().NotThrow($"status {status} carries cancel at period end");
                sub.CancelAtPeriodEnd.Should().BeTrue();
            }
            else
            {
                act.Should().Throw<BusinessRuleException>(because: $"status {status} cannot schedule cancellation");
                sub.CancelAtPeriodEnd.Should().BeFalse();
            }
        }
    }

    [Theory]
    [InlineData(SubscriptionStatus.Trialing)]
    [InlineData(SubscriptionStatus.Unpaid)]
    [InlineData(SubscriptionStatus.Incomplete)]
    public void ChangePlan_StillAllowedOnOpenStatuses(SubscriptionStatus status)
    {
        var sub = SubscriptionIn(status);

        sub.ChangePlan(Guid.NewGuid(), Actor, Now);

        sub.Status.Should().Be(status);
    }

    private static void Move(Subscription sub, SubscriptionStatus to)
    {
        switch (to)
        {
            case SubscriptionStatus.Active:
                sub.Renew(Now.AddDays(30), Now.AddDays(60), Actor, Now);
                break;
            case SubscriptionStatus.PastDue:
                sub.MarkPastDue(Actor, Now);
                break;
            case SubscriptionStatus.Canceled:
                sub.CancelImmediately(Actor, Now);
                break;
            case SubscriptionStatus.Expired:
                sub.Expire(Actor, Now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(to), to, "No aggregate method produces this status.");
        }
    }

    // Trialing, Unpaid and Incomplete have no creation path yet, so the status is loaded the way persistence
    // restores it (through the private setter) before the transition is tried.
    private static Subscription SubscriptionIn(SubscriptionStatus status)
    {
        var sub = Subscription.Create(Guid.NewGuid(), Guid.NewGuid(), SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);
        var setter = typeof(Subscription).GetProperty(nameof(Subscription.Status), BindingFlags.Instance | BindingFlags.Public)!
            .GetSetMethod(nonPublic: true)!;
        setter.Invoke(sub, new object[] { status });
        ((IHasDomainEvents)sub).ClearDomainEvents();
        return sub;
    }
}
