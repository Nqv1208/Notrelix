using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing;

public class UsageMetricTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var key = UsageMetricKey.Create("BOARD_COUNT");
        var period = UsagePeriod.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

        var metric = UsageMetric.Create(Guid.NewGuid(), workspaceId, key, period, DateTimeOffset.UtcNow);

        metric.WorkspaceId.Should().Be(workspaceId);
        metric.Key.Should().Be(key);
        metric.CurrentValue.Should().Be(0);
        metric.CurrentPeriod.Should().Be(period);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Increase(System.Int32,System.Int32,System.Boolean,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Increase_WithinLimit_ShouldSucceed_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(3, 5, isHardLimit: true, now);

        metric.CurrentValue.Should().Be(3);
        metric.History.Should().ContainSingle(h => h.Increment == 3);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricIncreasedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Increase(System.Int32,System.Int32,System.Boolean,System.DateTimeOffset)", MutationScenario.Invalid)]
    [CoversMutation(typeof(UsageMetric), "Increase(System.Int32,System.Int32,System.Boolean,System.DateTimeOffset)", MutationScenario.Event)]
    public void Increase_ExceedingHardLimit_ShouldThrowException_AndRaiseLimitExceededEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        var act = () => metric.Increase(6, 5, isHardLimit: true, now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*Usage limit exceeded*");
        metric.DomainEvents.Should().ContainSingle(e => e is UsageLimitExceededDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Increase(System.Int32,System.Int32,System.Boolean,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Increase_ExceedingSoftLimit_ShouldSucceed_AndRaiseBothEvents()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(6, 5, isHardLimit: false, now);

        metric.CurrentValue.Should().Be(6);
        metric.DomainEvents.Should().Contain(e => e is UsageLimitExceededDomainEvent);
        metric.DomainEvents.Should().Contain(e => e is UsageMetricIncreasedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Decrease(System.Int32,System.DateTimeOffset)", MutationScenario.Valid)]
    [CoversMutation(typeof(UsageMetric), "Decrease(System.Int32,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Decrease_ShouldReduceValue_AndNotAllowNegative()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(4, 10, isHardLimit: true, now);
        metric.Decrease(2, now);

        metric.CurrentValue.Should().Be(2);
        metric.History.Should().Contain(h => h.Increment == -2);

        var act = () => metric.Decrease(3, now);
        act.Should().Throw<DomainException>().WithMessage("Usage value cannot be negative.");
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Reset(Notrelix.Domain.Billing.Usage.UsagePeriod,System.DateTimeOffset)", MutationScenario.Valid)]
    public void Reset_ShouldClearValue()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(4, 10, isHardLimit: true, now);

        var nextPeriod = UsagePeriod.Create(now.AddDays(30), now.AddDays(60));
        metric.Reset(nextPeriod, now);

        metric.CurrentValue.Should().Be(0);
        metric.CurrentPeriod.Should().Be(nextPeriod);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Increase(System.Int32,System.Int32,System.Boolean,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Increase_WhenDeleted_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.SoftDelete(Guid.NewGuid(), now);

        var act = () => metric.Increase(3, 5, isHardLimit: true, now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Decrease(System.Int32,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Decrease_WhenDeleted_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.Increase(5, 10, isHardLimit: true, now);
        metric.SoftDelete(Guid.NewGuid(), now);

        var act = () => metric.Decrease(2, now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Decrease(System.Int32,System.DateTimeOffset)", MutationScenario.Invalid)]
    public void Decrease_WithNonPositiveAmount_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.Increase(5, 10, isHardLimit: true, now);

        var act = () => metric.Decrease(-2, now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Decrease(System.Int32,System.DateTimeOffset)", MutationScenario.Event)]
    public void Decrease_ShouldRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.Increase(5, 10, isHardLimit: true, now);
        ((IHasDomainEvents)metric).ClearDomainEvents();

        metric.Decrease(2, now);

        metric.DomainEvents.Should().Contain(e => e is UsageMetricDecreasedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    public void SoftDelete_ShouldMarkDeleted_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        ((IHasDomainEvents)metric).ClearDomainEvents();

        metric.SoftDelete(Guid.NewGuid(), now);

        metric.IsDeleted.Should().BeTrue();
        metric.DomainEvents.Should().Contain(e => e is UsageMetricSoftDeletedDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.NoOp)]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.SoftDelete(Guid.NewGuid(), now);
        ((IHasDomainEvents)metric).ClearDomainEvents();

        metric.SoftDelete(Guid.NewGuid(), now);

        metric.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    public void Restore_ShouldRestore_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.SoftDelete(Guid.NewGuid(), now);
        ((IHasDomainEvents)metric).ClearDomainEvents();

        metric.Restore(Guid.NewGuid(), now);

        metric.IsDeleted.Should().BeFalse();
        metric.DomainEvents.Should().Contain(e => e is UsageMetricRestoredDomainEvent);
    }

    [Fact]
    [CoversMutation(typeof(UsageMetric), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        ((IHasDomainEvents)metric).ClearDomainEvents();

        metric.Restore(Guid.NewGuid(), now);

        metric.DomainEvents.Should().BeEmpty();
    }
}
