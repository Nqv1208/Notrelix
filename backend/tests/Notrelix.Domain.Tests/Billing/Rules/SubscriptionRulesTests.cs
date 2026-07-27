using FluentAssertions;
using Notrelix.Domain.Billing.Rules;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Domain.Tests.Billing.Rules;

public class SubscriptionRulesTests
{
    [Theory]
    [InlineData(SubscriptionStatus.Trialing)]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Unpaid)]
    [InlineData(SubscriptionStatus.Incomplete)]
    public void EnsureCanChangePlan_WhenActive_ShouldNotThrow(SubscriptionStatus status)
    {
        Action act = () => SubscriptionRules.EnsureCanChangePlan(status);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(SubscriptionStatus.Canceled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void EnsureCanChangePlan_WhenInactive_ShouldThrow(SubscriptionStatus status)
    {
        Action act = () => SubscriptionRules.EnsureCanChangePlan(status);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*inactive*");
    }

    [Theory]
    [InlineData(SubscriptionStatus.Trialing)]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.PastDue)]
    [InlineData(SubscriptionStatus.Unpaid)]
    [InlineData(SubscriptionStatus.Incomplete)]
    public void EnsureCanCancel_WhenActive_ShouldNotThrow(SubscriptionStatus status)
    {
        Action act = () => SubscriptionRules.EnsureCanCancel(status);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(SubscriptionStatus.Canceled)]
    [InlineData(SubscriptionStatus.Expired)]
    public void EnsureCanCancel_WhenInactive_ShouldThrow(SubscriptionStatus status)
    {
        Action act = () => SubscriptionRules.EnsureCanCancel(status);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*inactive*");
    }

    [Fact]
    public void EnsurePeriodValid_WhenStartBeforeEnd_ShouldNotThrow()
    {
        var start = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);

        Action act = () => SubscriptionRules.EnsurePeriodValid(start, end);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePeriodValid_WhenStartEqualsEnd_ShouldThrow()
    {
        var time = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        Action act = () => SubscriptionRules.EnsurePeriodValid(time, time);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*start must be before end*");
    }

    [Fact]
    public void EnsurePeriodValid_WhenStartAfterEnd_ShouldThrow()
    {
        var start = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

        Action act = () => SubscriptionRules.EnsurePeriodValid(start, end);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*start must be before end*");
    }
}
