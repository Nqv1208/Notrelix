using FluentAssertions;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Widgets;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Analytics;

public class DashboardTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();

        var dashboard = Dashboard.Create(Guid.NewGuid(), workspaceId, "Sales Dashboard", actor, now);

        dashboard.WorkspaceId.Should().Be(workspaceId);
        dashboard.Name.Should().Be("Sales Dashboard");
        dashboard.Visibility.Should().Be(DashboardVisibility.Private);
        dashboard.Status.Should().Be(DashboardStatus.Active);
        dashboard.DomainEvents.Should().ContainSingle(e => e is DashboardCreatedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "Rename(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void Rename_ShouldUpdateName_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Old Name", actor, now);

        dashboard.Rename("New Name", actor, now);

        dashboard.Name.Should().Be("New Name");
        dashboard.DomainEvents.Should().Contain(e => e is DashboardRenamedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "ChangeVisibility(Notrelix.Domain.Analytics.Dashboards.DashboardVisibility,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void ChangeVisibility_ShouldUpdateVisibility_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);

        dashboard.ChangeVisibility(DashboardVisibility.Public, actor, now);

        dashboard.Visibility.Should().Be(DashboardVisibility.Public);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardVisibilityChangedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "AddWidget(System.String,Notrelix.Domain.Analytics.Dashboards.DashboardWidgetType,Notrelix.Domain.SharedKernel.JsonValue,Notrelix.Domain.Analytics.Widgets.WidgetPosition,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void AddWidget_WithInvalidPosition_ShouldThrowException()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);

        var act1 = () => dashboard.AddWidget("Test", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(-1, 0, 1, 1), actor, now);
        act1.Should().Throw<DomainException>().WithMessage("Widget coordinates (X, Y) must be non-negative.");

        var act2 = () => dashboard.AddWidget("Test", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 0, 1), actor, now);
        act2.Should().Throw<DomainException>().WithMessage("Widget dimensions (W, H) must be positive.");
    }

    [CoversMutation(typeof(Dashboard), "AddWidget(System.String,Notrelix.Domain.Analytics.Dashboards.DashboardWidgetType,Notrelix.Domain.SharedKernel.JsonValue,Notrelix.Domain.Analytics.Widgets.WidgetPosition,System.Guid,System.DateTimeOffset)", MutationScenario.Event)]
    [Fact]
    public void AddWidget_WithValidPosition_ShouldSucceed_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        var position = WidgetPosition.Create(0, 0, 2, 2);

        dashboard.AddWidget("Stats Widget", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), position, actor, now);

        dashboard.Widgets.Should().ContainSingle();
        dashboard.Widgets.First().Title.Should().Be("Stats Widget");
        dashboard.Widgets.First().Type.Should().Be(DashboardWidgetType.TextWidget);
        dashboard.Widgets.First().Position.Should().Be(position);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetAddedDomainEvent);
    }

    [Fact]
    public void MoveWidget_ShouldUpdatePosition_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        var position1 = WidgetPosition.Create(0, 0, 2, 2);
        var position2 = WidgetPosition.Create(2, 2, 4, 4);

        dashboard.AddWidget("Stats Widget", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), position1, actor, now);
        var widgetId = dashboard.Widgets.First().Id;

        dashboard.MoveWidget(widgetId, position2, actor, now);

        dashboard.Widgets.First().Position.Should().Be(position2);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetMovedDomainEvent);
    }

    [Fact]
    public void RemoveWidget_ShouldRemove_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        var position = WidgetPosition.Create(0, 0, 2, 2);

        dashboard.AddWidget("Stats Widget", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), position, actor, now);
        var widgetId = dashboard.Widgets.First().Id;

        dashboard.RemoveWidget(widgetId, actor, now);

        dashboard.Widgets.Should().BeEmpty();
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetRemovedDomainEvent);
    }

    [CoversMutation(typeof(Dashboard), "SoftDelete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [Fact]
    public void SoftDeleteAndRestore_ShouldUpdateStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);

        dashboard.SoftDelete(actor, now);
        dashboard.Status.Should().Be(DashboardStatus.Archived);
        dashboard.IsDeleted.Should().BeTrue();

        dashboard.Restore(actor, now);
        dashboard.Status.Should().Be(DashboardStatus.Active);
        dashboard.IsDeleted.Should().BeFalse();
    }
}
