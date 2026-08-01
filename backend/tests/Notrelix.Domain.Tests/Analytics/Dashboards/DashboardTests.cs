using FluentAssertions;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Widgets;

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
    public void Archive_ShouldSetStatusAndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);

        dashboard.Archive(actor, now.AddDays(1));

        dashboard.Status.Should().Be(DashboardStatus.Archived);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardArchivedDomainEvent);
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

    [Fact]
    public void RemoveWidget_WithMissingWidget_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);

        dashboard.RemoveWidget(Guid.NewGuid(), actor, now);

        dashboard.Widgets.Should().BeEmpty();
        dashboard.DomainEvents.Should().NotContain(e => e is DashboardWidgetRemovedDomainEvent);
    }

    [Fact]
    public void Archive_AlreadyArchived_ShouldBeNoOp()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        dashboard.Archive(actor, now);

        dashboard.Archive(actor, now.AddDays(1));

        dashboard.DomainEvents.OfType<DashboardCreatedDomainEvent>().Should().ContainSingle();
        dashboard.DomainEvents.OfType<DashboardArchivedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void AddWidget_OnArchivedDashboard_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        dashboard.Archive(actor, now);

        var act = () => dashboard.AddWidget("Widget", DashboardWidgetType.TextWidget, JsonValue.Create("{}"), WidgetPosition.Create(0, 0, 2, 2), actor, now);

        act.Should().Throw<DomainException>().WithMessage("Archived dashboards cannot be modified.");
    }

    [Fact]
    public void RemoveWidget_OnArchivedDashboard_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        dashboard.Archive(actor, now);

        var act = () => dashboard.RemoveWidget(Guid.NewGuid(), actor, now);

        act.Should().Throw<DomainException>().WithMessage("Archived dashboards cannot be modified.");
    }

    [Fact]
    public void MoveWidget_OnArchivedDashboard_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        dashboard.Archive(actor, now);

        var act = () => dashboard.MoveWidget(Guid.NewGuid(), WidgetPosition.Create(0, 0, 2, 2), actor, now);

        act.Should().Throw<DomainException>().WithMessage("Archived dashboards cannot be modified.");
    }

    [Fact]
    public void Rename_OnArchivedDashboard_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        dashboard.Archive(actor, now);

        var act = () => dashboard.Rename("New Name", actor, now);

        act.Should().Throw<DomainException>().WithMessage("Archived dashboards cannot be modified.");
    }

    [Fact]
    public void ChangeVisibility_OnArchivedDashboard_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        dashboard.Archive(actor, now);

        var act = () => dashboard.ChangeVisibility(DashboardVisibility.Public, actor, now);

        act.Should().Throw<DomainException>().WithMessage("Archived dashboards cannot be modified.");
    }

    [Fact]
    public void AddWidget_ExceedingLimit_ShouldNotAddWidgetOrRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);

        for (int i = 0; i < 50; i++)
        {
            dashboard.AddWidget($"Widget {i}", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(i % 10, i / 10, 2, 2), actor, now);
        }

        var act = () => dashboard.AddWidget("Overflow", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 2, 2), actor, now);
        act.Should().Throw<DomainException>();

        dashboard.Widgets.Should().HaveCount(50);
    }

    [Fact]
    public void RemoveWidget_InvalidTimestamp_ShouldNotRemoveWidget()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        dashboard.AddWidget("Widget", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 2, 2), actor, now);
        var widgetId = dashboard.Widgets.First().Id;

        var act = () => dashboard.RemoveWidget(widgetId, actor, default);
        act.Should().Throw<DomainException>();

        dashboard.Widgets.Should().ContainSingle();
    }

    [Fact]
    public void MoveWidget_InvalidTimestamp_ShouldNotChangePosition()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", actor, now);
        var initial = WidgetPosition.Create(0, 0, 2, 2);
        dashboard.AddWidget("Widget", DashboardWidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), initial, actor, now);
        var widgetId = dashboard.Widgets.First().Id;

        var next = WidgetPosition.Create(2, 2, 4, 4);
        var act = () => dashboard.MoveWidget(widgetId, next, actor, default);
        act.Should().Throw<DomainException>();

        dashboard.Widgets.First().Position.Should().Be(initial);
    }

}
