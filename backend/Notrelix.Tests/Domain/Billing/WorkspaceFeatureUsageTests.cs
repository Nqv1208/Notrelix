using FluentAssertions;
using Notrelix.Domain.Billing.Events;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Billing.Usage.Events;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Billing;

public class WorkspaceFeatureUsageTests
{
    private static readonly FeatureCode SampleFeature = FeatureCode.Create("BOARD_COUNT");

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, 80, DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(0);
        usage.HardLimit.Should().Be(100);
        usage.SoftLimit.Should().Be(80);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageInitializedEvent);
    }

    [Fact]
    public void Create_WithNegativeUsage_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, -1, 100, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithSoftLimitExceedingHardLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, 150, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Soft limit cannot exceed hard limit*");
    }

    [Fact]
    public void Consume_WithinLimit_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.ClearDomainEvents();

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
        usage.DomainEvents.Should().ContainSingle(e => e is FeatureUsageConsumedDomainEvent);
    }

    [Fact]
    public void Consume_ExceedingHardLimit_WhenOverageDisallowed_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 80, 100, null, DateTimeOffset.UtcNow);

        var act = () => usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*limit exceeded*");
        usage.DomainEvents.Should().ContainSingle(e => e is QuotaExceededDomainEvent);
    }

    [Fact]
    public void Consume_ExceedingHardLimit_WhenOverageAllowed_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 80, 100, null, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(110);
    }

    [Fact]
    public void Consume_WithNonPositiveAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Consume(-5, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Consume_WhenDeleted_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => usage.Consume(10, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Release_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        usage.ClearDomainEvents();

        usage.Release(20, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
        usage.DomainEvents.Should().ContainSingle(e => e is FeatureUsageReleasedDomainEvent);
    }

    [Fact]
    public void Release_WithAmountExceedingCurrent_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 10, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Release(20, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*below zero*");
    }

    [Fact]
    public void Release_WithNonPositiveAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Release(-5, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Reset_ShouldClearUsage_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 75, 100, null, DateTimeOffset.UtcNow);
        usage.ClearDomainEvents();

        usage.Reset(DateTimeOffset.UtcNow, Guid.NewGuid());

        usage.CurrentUsage.Should().Be(0);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageResetEvent);
    }

    [Fact]
    public void Release_WhenDeleted_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => usage.Release(10, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void SoftDelete_ShouldMarkDeleted_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.ClearDomainEvents();

        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.IsDeleted.Should().BeTrue();
        usage.DomainEvents.Should().Contain(e => e is WorkspaceFeatureUsageSoftDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        usage.ClearDomainEvents();

        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldRestore_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        usage.ClearDomainEvents();

        usage.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.IsDeleted.Should().BeFalse();
        usage.DomainEvents.Should().Contain(e => e is WorkspaceFeatureUsageRestoredEvent);
    }

    [Fact]
    public void SoftDelete_WhenConsumeAfterRestore_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        usage.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
    }

    [Fact]
    public void Create_WithNegativeSoftLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, -5, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithNegativeHardLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, -1, null, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithUsageExceedingHardLimit_AndOverageNotAllowed_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 150, 100, null, DateTimeOffset.UtcNow, overageAllowed: false);
        act.Should().Throw<BusinessRuleException>().WithMessage("*exceeds hard limit*");
    }

    [Fact]
    public void Create_WithUsageExceedingHardLimit_AndOverageAllowed_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 150, 100, null, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.CurrentUsage.Should().Be(150);
        usage.OverageAllowed.Should().BeTrue();
    }

    [Fact]
    public void Reset_ShouldClearUsage_AndSetLastResetAt()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 75, 100, null, DateTimeOffset.UtcNow);

        usage.Reset(DateTimeOffset.UtcNow, Guid.NewGuid());

        usage.CurrentUsage.Should().Be(0);
        usage.LastResetAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Consume_WhenUsageExceedsSoftLimit_ShouldNotThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 70, 100, 80, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.Consume(15, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(85);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.ClearDomainEvents();

        usage.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SoftDelete_AndRestore_ShouldToggleIsDeleted()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);

        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        usage.IsDeleted.Should().BeTrue();

        usage.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        usage.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Consume_WhenDeletedAndRestored_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        usage.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
    }

    [Fact]
    public void Release_WhenDeletedAndRestored_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        usage.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        usage.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.Release(20, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
    }
}
