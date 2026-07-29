using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Tests.Analytics.Dashboards;

[CoversAggregate(typeof(Dashboard))]
public class DashboardLifecycleTests
{
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actor = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [CoversMutation(typeof(Dashboard), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Rename_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "Old", _actor, _now);
        var versionBefore = dashboard.Version;

        dashboard.Rename("New", _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardRenamedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void Rename_SameName_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "Same", _actor, _now);
        var versionBefore = dashboard.Version;
        ((IHasDomainEvents)dashboard).ClearDomainEvents();

        dashboard.Rename("Same", _actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Dashboard), "ChangeVisibility(Notrelix.Domain.Analytics.Dashboards.DashboardVisibility,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void ChangeVisibility_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        var versionBefore = dashboard.Version;

        dashboard.ChangeVisibility(DashboardVisibility.Public, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardVisibilityChangedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "ChangeVisibility(Notrelix.Domain.Analytics.Dashboards.DashboardVisibility,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void ChangeVisibility_SameValue_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.ChangeVisibility(DashboardVisibility.Private, _actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Dashboard), "AddWidget(System.String,Notrelix.Domain.Analytics.Dashboards.DashboardWidgetType,Notrelix.Domain.SharedKernel.JsonValue,Notrelix.Domain.Analytics.Widgets.WidgetPosition,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [CoversMutation(typeof(Dashboard), "RemoveWidget(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void AddWidget_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        var versionBefore = dashboard.Version;
        var pos = WidgetPosition.Create(0, 0, 2, 2);

        dashboard.AddWidget("Stats", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"stats\"}"), pos, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetAddedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "RemoveWidget(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void RemoveWidget_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        var pos = WidgetPosition.Create(0, 0, 2, 2);
        dashboard.AddWidget("Stats", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"stats\"}"), pos, _actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;
        var widgetId = dashboard.Widgets.First().Id;

        dashboard.RemoveWidget(widgetId, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetRemovedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "RemoveWidget(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.NoOp)]
    [Fact]
    public void RemoveWidget_UnknownId_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.RemoveWidget(Guid.NewGuid(), _actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Dashboard), "MoveWidget(System.Guid,Notrelix.Domain.Analytics.Widgets.WidgetPosition,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [CoversMutation(typeof(Dashboard), "RemoveWidget(System.Guid,System.Guid,System.DateTimeOffset)", MutationScenario.Version)]
    [Fact]
    public void MoveWidget_ShouldIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        var pos1 = WidgetPosition.Create(0, 0, 2, 2);
        dashboard.AddWidget("Stats", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"stats\"}"), pos1, _actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;
        var widgetId = dashboard.Widgets.First().Id;
        var pos2 = WidgetPosition.Create(2, 2, 4, 4);

        dashboard.MoveWidget(widgetId, pos2, _actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetMovedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_ShouldIncrementVersion_AndRaiseDeletedEvent()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.SoftDelete(_actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.IsDeleted.Should().BeTrue();
        dashboard.Status.Should().Be(DashboardStatus.Archived);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardDeletedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_ShouldIncrementVersion_AndRaiseRestoredEvent()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        dashboard.SoftDelete(_actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.Restore(_actor, _now);

        dashboard.Version.Should().Be(versionBefore + 1);
        dashboard.IsDeleted.Should().BeFalse();
        dashboard.Status.Should().Be(DashboardStatus.Active);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardRestoredDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDelete_Twice_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        dashboard.SoftDelete(_actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.SoftDelete(_actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(Dashboard), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
    [Fact]
    public void Restore_Twice_ShouldNotIncrementVersion()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), _workspaceId, "D", _actor, _now);
        dashboard.SoftDelete(_actor, _now);
        dashboard.Restore(_actor, _now);
        ((IHasDomainEvents)dashboard).ClearDomainEvents();
        var versionBefore = dashboard.Version;

        dashboard.Restore(_actor, _now);

        dashboard.Version.Should().Be(versionBefore);
        dashboard.DomainEvents.Should().BeEmpty();
    }
}
