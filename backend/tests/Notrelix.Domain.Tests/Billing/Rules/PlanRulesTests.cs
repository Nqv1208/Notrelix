using FluentAssertions;
using Notrelix.Domain.Billing.Rules;

namespace Notrelix.Domain.Tests.Billing.Rules;

public class PlanRulesTests
{
    [Fact]
    public void EnsurePricePositive_WhenPositive_ShouldNotThrow()
    {
        var price = Money.Create(9.99m, "USD");

        Action act = () => PlanRules.EnsurePricePositive(price);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePricePositive_WhenZero_ShouldNotThrow()
    {
        var price = Money.Create(0m, "USD");

        Action act = () => PlanRules.EnsurePricePositive(price);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePricePositive_WhenNegative_ShouldThrow()
    {
        var price = Money.Create(-5m, "USD");

        Action act = () => PlanRules.EnsurePricePositive(price);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*negative*");
    }

    [Fact]
    public void EnsureNameNotTooLong_WhenWithinLimit_ShouldNotThrow()
    {
        Action act = () => PlanRules.EnsureNameNotTooLong("Pro Plan");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureNameNotTooLong_WhenExceedsLimit_ShouldThrow()
    {
        var longName = new string('A', 101);

        Action act = () => PlanRules.EnsureNameNotTooLong(longName);

        act.Should().Throw<BusinessRuleException>();
    }
}
