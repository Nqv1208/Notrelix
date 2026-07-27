using FluentAssertions;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing;

public class PlanVersionTests
{
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void AddLimit_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        ((IHasDomainEvents)plan).ClearDomainEvents();
        var version = plan.Version;

        plan.AddLimit(FeatureCode.Create("seats"), 10, _now);

        plan.Version.Should().Be(version + 1);
    }

    [Fact]
    public void UpdateDescription_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        ((IHasDomainEvents)plan).ClearDomainEvents();
        var version = plan.Version;

        plan.UpdateDescription("New desc", _now);

        plan.Version.Should().Be(version + 1);
    }

    [Fact]
    public void Archive_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        ((IHasDomainEvents)plan).ClearDomainEvents();
        var version = plan.Version;

        plan.Archive(_now);

        plan.Version.Should().Be(version + 1);
    }

    [Fact]
    public void Deprecate_ShouldIncrementVersion()
    {
        var plan = Plan.Create("Pro", Money.Create(29, "USD"), BillingPeriod.Monthly, _now);
        ((IHasDomainEvents)plan).ClearDomainEvents();
        var version = plan.Version;

        plan.Deprecate(_now);

        plan.Version.Should().Be(version + 1);
    }
}
