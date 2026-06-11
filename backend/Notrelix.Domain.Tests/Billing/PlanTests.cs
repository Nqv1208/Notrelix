using FluentAssertions;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;
using Xunit;

namespace Notrelix.Domain.Tests.Billing;

public class PlanTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var price = Money.Create(19.99m, "USD");
        var plan = Plan.Create("Pro Plan", price, BillingPeriod.Monthly);

        plan.Name.Should().Be("Pro Plan");
        plan.Price.Should().Be(price);
        plan.Status.Should().Be(PlanStatus.Active);
    }

    [Fact]
    public void AddLimit_ShouldAddToList()
    {
        var plan = Plan.Create("Free", Money.Create(0, "USD"), BillingPeriod.Monthly);
        var feature = FeatureCode.Create("BOARD_COUNT");

        plan.AddLimit(feature, 5);

        plan.Limits.Should().HaveCount(1);
        plan.Limits.First().Feature.Should().Be(feature);
        plan.Limits.First().Limit.Should().Be(5);
    }
}
