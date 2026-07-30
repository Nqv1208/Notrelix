using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing;

[CoversAggregate(typeof(WorkspaceFeatureUsage))]
public class WorkspaceFeatureUsageTests
{
    private static readonly FeatureCode SampleFeature = FeatureCode.Create("BOARD_COUNT");

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, 80, DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(0);
        usage.HardLimit.Should().Be(100);
        usage.SoftLimit.Should().Be(80);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageInitializedDomainEvent);
    }

    [Fact]
    public void Create_WithNegativeUsage_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, -1, 100, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithSoftLimitExceedingHardLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, 150, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Soft limit cannot exceed hard limit*");
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Consume(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Consume_WithinLimit_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)usage).ClearDomainEvents();

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
        usage.DomainEvents.Should().ContainSingle(e => e is FeatureUsageConsumedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Consume(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Consume(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    public void Consume_ExceedingHardLimit_WhenOverageDisallowed_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 80, 100, null, DateTimeOffset.UtcNow);

        var act = () => usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*limit exceeded*");
        usage.DomainEvents.Should().ContainSingle(e => e is QuotaExceededDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Consume(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Consume_ExceedingHardLimit_WhenOverageAllowed_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 80, 100, null, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(110);
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Consume(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Consume_WithNonPositiveAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Consume(-5, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Release(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Release_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)usage).ClearDomainEvents();

        usage.Release(20, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
        usage.DomainEvents.Should().ContainSingle(e => e is FeatureUsageReleasedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Release(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Release_WithAmountExceedingCurrent_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 10, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Release(20, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*below zero*");
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Release(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Release_WithNonPositiveAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Release(-5, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Reset(System.DateTimeOffset,System.Guid)", MutationScenario.Valid)]
    public void Reset_ShouldClearUsage_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 75, 100, null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)usage).ClearDomainEvents();

        usage.Reset(DateTimeOffset.UtcNow, Guid.NewGuid());

        usage.CurrentUsage.Should().Be(0);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageResetDomainEvent);
    }

    [Fact]
    public void Create_WithNegativeSoftLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, -5, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithNegativeHardLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, -1, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithUsageExceedingHardLimit_AndOverageNotAllowed_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 150, 100, null, DateTimeOffset.UtcNow, overageAllowed: false);
        act.Should().Throw<BusinessRuleException>().WithMessage("*exceeds hard limit*");
    }

    [Fact]
    public void Create_WithUsageExceedingHardLimit_AndOverageAllowed_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 150, 100, null, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.CurrentUsage.Should().Be(150);
        usage.OverageAllowed.Should().BeTrue();
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Reset(System.DateTimeOffset,System.Guid)", MutationScenario.Valid)]
    public void Reset_ShouldClearUsage_AndSetLastResetAt()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 75, 100, null, DateTimeOffset.UtcNow);

        usage.Reset(DateTimeOffset.UtcNow, Guid.NewGuid());

        usage.CurrentUsage.Should().Be(0);
        usage.LastResetAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    [CoversMutation(typeof(WorkspaceFeatureUsage), "Consume(System.Decimal,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Consume_WhenUsageExceedsSoftLimit_ShouldNotThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 70, 100, 80, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.Consume(15, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(85);
    }

}
