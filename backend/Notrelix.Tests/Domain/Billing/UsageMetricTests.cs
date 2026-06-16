using FluentAssertions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Billing;

public class UsageMetricTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var workspaceId = Guid.NewGuid();
        var key = UsageMetricKey.Create("BOARD_COUNT");
        var period = UsagePeriod.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));

        var metric = UsageMetric.Create(workspaceId, key, period, DateTimeOffset.UtcNow);

        metric.WorkspaceId.Should().Be(workspaceId);
        metric.Key.Should().Be(key);
        metric.CurrentValue.Should().Be(0);
        metric.CurrentPeriod.Should().Be(period);
    }

    [Fact]
    public void Increase_WithinLimit_ShouldSucceed_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(3, 5, isHardLimit: true, now);

        metric.CurrentValue.Should().Be(3);
        metric.History.Should().ContainSingle(h => h.Increment == 3);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricIncreasedEvent);
    }

    [Fact]
    public void Increase_ExceedingHardLimit_ShouldThrowException_AndRaiseLimitExceededEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        var act = () => metric.Increase(6, 5, isHardLimit: true, now);

        act.Should().Throw<BusinessRuleException>().WithMessage("*Usage limit exceeded*");
        metric.DomainEvents.Should().ContainSingle(e => e is UsageLimitExceededEvent);
    }

    [Fact]
    public void Increase_ExceedingSoftLimit_ShouldSucceed_AndRaiseBothEvents()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(6, 5, isHardLimit: false, now);

        metric.CurrentValue.Should().Be(6);
        metric.DomainEvents.Should().Contain(e => e is UsageLimitExceededEvent);
        metric.DomainEvents.Should().Contain(e => e is UsageMetricIncreasedEvent);
    }

    [Fact]
    public void Decrease_ShouldReduceValue_AndNotAllowNegative()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(4, 10, isHardLimit: true, now);
        metric.Decrease(2, now);

        metric.CurrentValue.Should().Be(2);
        metric.History.Should().Contain(h => h.Increment == -2);

        var act = () => metric.Decrease(3, now);
        act.Should().Throw<DomainException>().WithMessage("Usage value cannot be negative.");
    }

    [Fact]
    public void Reset_ShouldClearValue()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);

        metric.Increase(4, 10, isHardLimit: true, now);
        
        var nextPeriod = UsagePeriod.Create(now.AddDays(30), now.AddDays(60));
        metric.Reset(nextPeriod, now);

        metric.CurrentValue.Should().Be(0);
        metric.CurrentPeriod.Should().Be(nextPeriod);
    }

    [Fact]
    public void Increase_WhenDeleted_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.SoftDelete(Guid.NewGuid(), now);

        var act = () => metric.Increase(3, 5, isHardLimit: true, now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Decrease_WhenDeleted_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.Increase(5, 10, isHardLimit: true, now);
        metric.SoftDelete(Guid.NewGuid(), now);

        var act = () => metric.Decrease(2, now);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Decrease_WithNonPositiveAmount_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.Increase(5, 10, isHardLimit: true, now);

        var act = () => metric.Decrease(-2, now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Decrease_ShouldRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.Increase(5, 10, isHardLimit: true, now);
        metric.ClearDomainEvents();

        metric.Decrease(2, now);

        metric.DomainEvents.Should().Contain(e => e is UsageMetricDecreasedEvent);
    }

    [Fact]
    public void SoftDelete_ShouldMarkDeleted_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.ClearDomainEvents();

        metric.SoftDelete(Guid.NewGuid(), now);

        metric.IsDeleted.Should().BeTrue();
        metric.DomainEvents.Should().Contain(e => e is UsageMetricSoftDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.SoftDelete(Guid.NewGuid(), now);
        metric.ClearDomainEvents();

        metric.SoftDelete(Guid.NewGuid(), now);

        metric.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldRestore_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.SoftDelete(Guid.NewGuid(), now);
        metric.ClearDomainEvents();

        metric.Restore(Guid.NewGuid(), now);

        metric.IsDeleted.Should().BeFalse();
        metric.DomainEvents.Should().Contain(e => e is UsageMetricRestoredEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var metric = UsageMetric.Create(Guid.NewGuid(), UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(now, now.AddDays(30)), now);
        metric.ClearDomainEvents();

        metric.Restore(Guid.NewGuid(), now);

        metric.DomainEvents.Should().BeEmpty();
    }
}
