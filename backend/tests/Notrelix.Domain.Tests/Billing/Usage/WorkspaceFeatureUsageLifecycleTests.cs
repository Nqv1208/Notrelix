using FluentAssertions;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing.Usage;

public class WorkspaceFeatureUsageLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void WorkspaceFeatureUsage_Create_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, feature, 0, 100, null, Now);

        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageInitializedDomainEvent);
        var evt = (WorkspaceFeatureUsageInitializedDomainEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageInitializedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.CurrentUsage.Should().Be(0);
        evt.HardLimit.Should().Be(100);
    }

    [Fact]
    public void WorkspaceFeatureUsage_Reset_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, feature, 50, 100, null, Now);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var version = usage.Version;

        usage.Reset(Now, Actor);

        usage.Version.Should().Be(version + 1);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageResetDomainEvent);
        var evt = (WorkspaceFeatureUsageResetDomainEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageResetDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
    }

    [Fact]
    public void WorkspaceFeatureUsage_SoftDelete_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, feature, 0, 100, null, Now);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var version = usage.Version;

        usage.SoftDelete(Actor, Now);

        usage.IsDeleted.Should().BeTrue();
        usage.Version.Should().Be(version + 1);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageSoftDeletedDomainEvent);
        var evt = (WorkspaceFeatureUsageSoftDeletedDomainEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageSoftDeletedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.DeletedBy.Should().Be(Actor);
    }

    [Fact]
    public void WorkspaceFeatureUsage_Restore_ShouldRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, feature, 0, 100, null, Now);
        usage.SoftDelete(Actor, Now);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var version = usage.Version;

        usage.Restore(Actor, Now);

        usage.IsDeleted.Should().BeFalse();
        usage.Version.Should().Be(version + 1);
        usage.DomainEvents.Should().ContainSingle(e => e is WorkspaceFeatureUsageRestoredDomainEvent);
        var evt = (WorkspaceFeatureUsageRestoredDomainEvent)usage.DomainEvents.Single(e => e is WorkspaceFeatureUsageRestoredDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.RestoredBy.Should().Be(Actor);
    }

    [Fact]
    public void WorkspaceFeatureUsage_SoftDelete_WhenAlreadyDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, feature, 0, 100, null, Now);
        usage.SoftDelete(Actor, Now);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var version = usage.Version;

        usage.SoftDelete(Actor, Now);

        usage.Version.Should().Be(version);
        usage.DomainEvents.Should().NotContain(e => e is WorkspaceFeatureUsageSoftDeletedDomainEvent);
    }

    [Fact]
    public void WorkspaceFeatureUsage_Restore_WhenNotDeleted_ShouldNotRaiseEvent()
    {
        var feature = FeatureCode.Create("storage");
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, feature, 0, 100, null, Now);
        ((IHasDomainEvents)usage).ClearDomainEvents();
        var version = usage.Version;

        usage.Restore(Actor, Now);

        usage.Version.Should().Be(version);
        usage.DomainEvents.Should().NotContain(e => e is WorkspaceFeatureUsageRestoredDomainEvent);
    }
}
