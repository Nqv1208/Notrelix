using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing;

[CoversAggregate(typeof(Plan))]
public class PlanTests
{
    private static readonly Money SamplePrice = Money.Create(19.99m, "USD");
    private static readonly FeatureCode SampleFeature = FeatureCode.Create("BOARD_COUNT");

    [Fact]
    public void Create_ShouldSucceed()
    {
        var plan = Plan.Create("Pro Plan", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);

        plan.Name.Should().Be("Pro Plan");
        plan.Price.Should().Be(SamplePrice);
        plan.Status.Should().Be(PlanStatus.Active);
    }

    [Fact]
    public void AddLimit_ShouldAddToList()
    {
        var plan = Plan.Create("Free", Money.Create(0, "USD"), BillingPeriod.Monthly, DateTimeOffset.UtcNow);

        plan.AddLimit(SampleFeature, 5, DateTimeOffset.UtcNow);

        plan.Limits.Should().HaveCount(1);
        plan.Limits.First().Feature.Should().Be(SampleFeature);
        plan.Limits.First().Limit.Should().Be(5);
    }

    [Fact]
    public void AddLimit_DuplicateFeature_ShouldThrow()
    {
        var plan = Plan.Create("Pro", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);
        plan.AddLimit(SampleFeature, 10, DateTimeOffset.UtcNow);

        var act = () => plan.AddLimit(SampleFeature, 20, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*already added*");
    }

    [Fact]
    public void AddLimit_ShouldRaiseEvent()
    {
        var plan = Plan.Create("Pro", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)plan).ClearDomainEvents();

        plan.AddLimit(SampleFeature, 5, DateTimeOffset.UtcNow);

        plan.DomainEvents.Should().ContainSingle(e => e is PlanLimitAddedDomainEvent);
    }

    [Fact]
    public void UpdateDescription_ShouldUpdate()
    {
        var plan = Plan.Create("Pro", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);

        plan.UpdateDescription("Premium plan", DateTimeOffset.UtcNow);

        plan.Description.Should().Be("Premium plan");
    }

    [Fact]
    public void UpdateDescription_WithNull_ShouldSetNull()
    {
        var plan = Plan.Create("Pro", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);
        plan.UpdateDescription("Desc", DateTimeOffset.UtcNow);

        plan.UpdateDescription(null, DateTimeOffset.UtcNow);

        plan.Description.Should().BeNull();
    }

    [Fact]
    public void Archive_ShouldTransition_AndRaiseEvent()
    {
        var plan = Plan.Create("Legacy", SamplePrice, BillingPeriod.Yearly, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)plan).ClearDomainEvents();

        plan.Archive(DateTimeOffset.UtcNow);

        plan.Status.Should().Be(PlanStatus.Archived);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanArchivedDomainEvent);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ShouldBeNoOp()
    {
        var plan = Plan.Create("Legacy", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);
        plan.Archive(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)plan).ClearDomainEvents();

        plan.Archive(DateTimeOffset.UtcNow);

        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Deprecate_ShouldTransition_AndRaiseEvent()
    {
        var plan = Plan.Create("Old", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)plan).ClearDomainEvents();

        plan.Deprecate(DateTimeOffset.UtcNow);

        plan.Status.Should().Be(PlanStatus.Deprecated);
        plan.DomainEvents.Should().ContainSingle(e => e is PlanDeprecatedDomainEvent);
    }

    [Fact]
    public void Deprecate_WhenAlreadyDeprecated_ShouldBeNoOp()
    {
        var plan = Plan.Create("Old", SamplePrice, BillingPeriod.Monthly, DateTimeOffset.UtcNow);
        plan.Deprecate(DateTimeOffset.UtcNow);
        ((IHasDomainEvents)plan).ClearDomainEvents();

        plan.Deprecate(DateTimeOffset.UtcNow);

        plan.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void PlanLimit_Create_WithNegativeLimit_ShouldThrow()
    {
        var act = () => PlanLimit.Create(Guid.NewGuid(), SampleFeature, -1);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void PlanLimit_UpdateLimit_WithNegative_ShouldThrow()
    {
        var limit = PlanLimit.Create(Guid.NewGuid(), SampleFeature, 10);
        var act = () => limit.UpdateLimit(-5);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void PlanLimit_UpdateLimit_ShouldUpdate()
    {
        var limit = PlanLimit.Create(Guid.NewGuid(), SampleFeature, 10);

        limit.UpdateLimit(25);

        limit.Limit.Should().Be(25);
    }
}
