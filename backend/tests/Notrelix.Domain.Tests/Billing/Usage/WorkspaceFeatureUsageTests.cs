using FluentAssertions;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing;

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
    public void Consume_WithinLimit_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)usage).ClearDomainEvents();

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
        usage.DomainEvents.Should().ContainSingle(e => e is FeatureUsageConsumedDomainEvent);
    }

    [Fact]
    public void Consume_ExceedingHardLimit_WhenOverageDisallowed_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 80, 100, null, DateTimeOffset.UtcNow);

        var act = () => usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*limit exceeded*");
        usage.DomainEvents.Should().ContainSingle(e => e is QuotaExceededDomainEvent);
    }

    [Fact]
    public void Consume_ExceedingHardLimit_WhenOverageAllowed_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 80, 100, null, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.Consume(30, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(110);
    }

    [Fact]
    public void Consume_WithNonPositiveAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Consume(-5, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Release_ShouldSucceed_AndRaiseEvent()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)usage).ClearDomainEvents();

        usage.Release(20, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(30);
        usage.DomainEvents.Should().ContainSingle(e => e is FeatureUsageReleasedDomainEvent);
    }

    [Fact]
    public void Release_WithAmountExceedingCurrent_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 10, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Release(20, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*below zero*");
    }

    [Fact]
    public void Release_WithNonPositiveAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 50, 100, null, DateTimeOffset.UtcNow);
        var act = () => usage.Release(-5, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
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
    public void Reset_ShouldClearUsage_AndSetLastResetAt()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 75, 100, null, DateTimeOffset.UtcNow);

        usage.Reset(DateTimeOffset.UtcNow, Guid.NewGuid());

        usage.CurrentUsage.Should().Be(0);
        usage.LastResetAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Consume_WhenUsageExceedsSoftLimit_ShouldNotThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 70, 100, 80, DateTimeOffset.UtcNow, overageAllowed: true);

        usage.Consume(15, Guid.NewGuid(), DateTimeOffset.UtcNow);

        usage.CurrentUsage.Should().Be(85);
    }

    [Fact]
    public void ReconfigureLimits_WithNewLimits_ShouldUpdateBoth_RaiseEvent_AndAdvanceVersion()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, occurredAt);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var versionBefore = usage.Version;

        usage.ReconfigureLimits(80, 70, actor, occurredAt);

        usage.HardLimit.Should().Be(80);
        usage.SoftLimit.Should().Be(70);
        usage.CurrentUsage.Should().Be(0, "reconfiguring limits never touches committed usage");
        usage.Version.Should().Be(versionBefore + 1);
        var reconfigured = usage.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceFeatureUsageLimitsReconfiguredDomainEvent>().Subject;
        reconfigured.HardLimit.Should().Be(80);
        reconfigured.SoftLimit.Should().Be(70);
    }

    [Fact]
    public void ReconfigureLimits_WithSameLimits_ShouldBeNoOp_NoVersionIncrement_NoEvent()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, 80, occurredAt);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var versionBefore = usage.Version;

        usage.ReconfigureLimits(100, 80, actor, occurredAt);

        usage.HardLimit.Should().Be(100);
        usage.SoftLimit.Should().Be(80);
        usage.Version.Should().Be(versionBefore, "an unchanged limit is a semantic no-op");
        usage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ReconfigureLimits_DowngradeBelowCurrentUsage_ShouldRetainUsage_NotThrow_AndAdvanceVersion()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 150, 100, 90, occurredAt, overageAllowed: true);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var versionBefore = usage.Version;

        usage.ReconfigureLimits(80, 70, actor, occurredAt);

        usage.CurrentUsage.Should().Be(150, "a limit downgrade never deletes committed usage (transient over-limit)");
        usage.HardLimit.Should().Be(80);
        usage.SoftLimit.Should().Be(70);
        usage.Version.Should().Be(versionBefore + 1);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageLimitsReconfiguredDomainEvent);
    }

    [Fact]
    public void ReconfigureLimits_ConsumeExceedingDowngradedLimit_ShouldThrow_RetainingSeededUsage()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, occurredAt);

        usage.ReconfigureLimits(2, 2, actor, occurredAt);
        var act = () => usage.Consume(3, actor, occurredAt);

        act.Should().Throw<BusinessRuleException>().WithMessage("*limit exceeded*");
        usage.CurrentUsage.Should().Be(0, "a denied consume must leave usage unchanged");
        usage.HardLimit.Should().Be(2);
    }

    [Fact]
    public void ReconfigureLimits_ToNull_ShouldClearNumericCeiling()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 50, 100, 100, occurredAt);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var versionBefore = usage.Version;

        usage.ReconfigureLimits(null, null, actor, occurredAt);

        usage.HardLimit.Should().BeNull("an unlimited grant clears the numeric ceiling");
        usage.SoftLimit.Should().BeNull();
        usage.Version.Should().Be(versionBefore + 1);
        var unlimited = usage.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<WorkspaceFeatureUsageLimitsReconfiguredDomainEvent>().Subject;
        unlimited.HardLimit.Should().BeNull();
        unlimited.SoftLimit.Should().BeNull();
    }

    [Fact]
    public void ReconfigureLimits_WithNegativeHardLimit_ShouldThrow_NoMutation()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, occurredAt);
        var versionBefore = usage.Version;

        var act = () => usage.ReconfigureLimits(-1, null, actor, occurredAt);

        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
        usage.HardLimit.Should().Be(100);
        usage.Version.Should().Be(versionBefore);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageInitializedDomainEvent);
    }

    [Fact]
    public void ReconfigureLimits_WithNegativeSoftLimit_ShouldThrow_NoMutation()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, occurredAt);
        var versionBefore = usage.Version;

        var act = () => usage.ReconfigureLimits(null, -1, actor, occurredAt);

        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
        usage.SoftLimit.Should().BeNull();
        usage.Version.Should().Be(versionBefore);
    }

    [Fact]
    public void ReconfigureLimits_WithSoftLimitExceedingHardLimit_ShouldThrow_NoMutation()
    {
        var actor = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), Guid.NewGuid(), SampleFeature, 0, 100, null, occurredAt);
        var versionBefore = usage.Version;

        var act = () => usage.ReconfigureLimits(100, 150, actor, occurredAt);

        act.Should().Throw<BusinessRuleException>().WithMessage("*Soft limit cannot exceed hard limit*");
        usage.Version.Should().Be(versionBefore);
    }

}
