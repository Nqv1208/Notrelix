using FluentAssertions;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing.Usage;

public class UsageMetricLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void UsageMetric_Create_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(Guid.NewGuid(), WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);

        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricCreatedDomainEvent);
        var evt = (UsageMetricCreatedDomainEvent)metric.DomainEvents.Single(e => e is UsageMetricCreatedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.Key.Should().Be(metric.Key);
    }

    [Fact]
    public void UsageMetric_Decrease_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(Guid.NewGuid(), WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.ClearDomainEvents();
        metric.Increase(5, 10, isHardLimit: true, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Decrease(2, Now);

        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricDecreasedDomainEvent);
        var evt = (UsageMetricDecreasedDomainEvent)metric.DomainEvents.Single(e => e is UsageMetricDecreasedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.Amount.Should().Be(2);
    }

    [Fact]
    public void UsageMetric_Reset_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(Guid.NewGuid(), WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.Increase(5, 10, isHardLimit: true, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Reset(UsagePeriod.Create(Now.AddDays(30), Now.AddDays(60)), Now);

        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricResetDomainEvent);
        var evt = (UsageMetricResetDomainEvent)metric.DomainEvents.Single(e => e is UsageMetricResetDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void UsageMetric_SoftDelete_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(Guid.NewGuid(), WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.SoftDelete(Actor, Now);

        metric.IsDeleted.Should().BeTrue();
        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricSoftDeletedDomainEvent);
        var evt = (UsageMetricSoftDeletedDomainEvent)metric.DomainEvents.Single(e => e is UsageMetricSoftDeletedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void UsageMetric_Restore_ShouldRaiseEvent()
    {
        var metric = UsageMetric.Create(Guid.NewGuid(), WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.SoftDelete(Actor, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Restore(Actor, Now);

        metric.IsDeleted.Should().BeFalse();
        metric.Version.Should().Be(version + 1);
        metric.DomainEvents.Should().ContainSingle(e => e is UsageMetricRestoredDomainEvent);
        var evt = (UsageMetricRestoredDomainEvent)metric.DomainEvents.Single(e => e is UsageMetricRestoredDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void UsageMetric_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var metric = UsageMetric.Create(Guid.NewGuid(), WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.SoftDelete(Actor, Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.SoftDelete(Actor, Now);

        metric.Version.Should().Be(version);
        metric.DomainEvents.Should().NotContain(e => e is UsageMetricSoftDeletedDomainEvent);
    }

    [Fact]
    public void UsageMetric_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var metric = UsageMetric.Create(Guid.NewGuid(), WsA, UsageMetricKey.Create("BOARD_COUNT"), UsagePeriod.Create(Now, Now.AddDays(30)), Now);
        metric.ClearDomainEvents();
        var version = metric.Version;

        metric.Restore(Actor, Now);

        metric.Version.Should().Be(version);
        metric.DomainEvents.Should().NotContain(e => e is UsageMetricRestoredDomainEvent);
    }
}
