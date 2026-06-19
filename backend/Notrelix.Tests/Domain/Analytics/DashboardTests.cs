using FluentAssertions;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Widgets;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.SharedKernel;
using Xunit;

using WidgetType = Notrelix.Domain.Analytics.Dashboards.WidgetType;

namespace Notrelix.Domain.Tests.Analytics;

public class DashboardTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();

        var dashboard = Dashboard.Create(workspaceId, "Sales Dashboard", actor, now);

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
        var dashboard = Dashboard.Create(Guid.NewGuid(), "Old Name", actor, now);

        dashboard.Rename("New Name", actor, now);

        dashboard.Name.Should().Be("New Name");
        dashboard.DomainEvents.Should().Contain(e => e is DashboardRenamedDomainEvent);
    }

    [Fact]
    public void ChangeVisibility_ShouldUpdateVisibility_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), "Dashboard", actor, now);

        dashboard.ChangeVisibility(DashboardVisibility.Public, actor, now);

        dashboard.Visibility.Should().Be(DashboardVisibility.Public);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardVisibilityChangedDomainEvent);
    }

    [Fact]
    public void AddWidget_WithInvalidPosition_ShouldThrowException()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), "Dashboard", actor, now);

        var act1 = () => dashboard.AddWidget("Test", WidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(-1, 0, 1, 1), actor, now);
        act1.Should().Throw<DomainException>().WithMessage("Widget coordinates (X, Y) must be non-negative.");

        var act2 = () => dashboard.AddWidget("Test", WidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 0, 1), actor, now);
        act2.Should().Throw<DomainException>().WithMessage("Widget dimensions (W, H) must be positive.");
    }

    [Fact]
    public void AddWidget_WithValidPosition_ShouldSucceed_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), "Dashboard", actor, now);
        var position = WidgetPosition.Create(0, 0, 2, 2);

        dashboard.AddWidget("Stats Widget", WidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), position, actor, now);

        dashboard.Widgets.Should().ContainSingle();
        dashboard.Widgets.First().Title.Should().Be("Stats Widget");
        dashboard.Widgets.First().Type.Should().Be(WidgetType.TextWidget);
        dashboard.Widgets.First().Position.Should().Be(position);
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetAddedDomainEvent);
    }

    [Fact]
    public void MoveWidget_ShouldUpdatePosition_AndRaiseEvent()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), "Dashboard", actor, now);
        var position1 = WidgetPosition.Create(0, 0, 2, 2);
        var position2 = WidgetPosition.Create(2, 2, 4, 4);

        dashboard.AddWidget("Stats Widget", WidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), position1, actor, now);
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
        var dashboard = Dashboard.Create(Guid.NewGuid(), "Dashboard", actor, now);
        var position = WidgetPosition.Create(0, 0, 2, 2);

        dashboard.AddWidget("Stats Widget", WidgetType.TextWidget, JsonValue.Create("{\"content\":\"test\"}"), position, actor, now);
        var widgetId = dashboard.Widgets.First().Id;

        dashboard.RemoveWidget(widgetId, actor, now);

        dashboard.Widgets.Should().BeEmpty();
        dashboard.DomainEvents.Should().Contain(e => e is DashboardWidgetRemovedDomainEvent);
    }

    [Fact]
    public void SoftDeleteAndRestore_ShouldUpdateStatus()
    {
        var now = DateTimeOffset.UtcNow;
        var actor = Guid.NewGuid();
        var dashboard = Dashboard.Create(Guid.NewGuid(), "Dashboard", actor, now);

        dashboard.SoftDelete(actor, now);
        dashboard.Status.Should().Be(DashboardStatus.Archived);
        dashboard.IsDeleted.Should().BeTrue();

        dashboard.Restore(actor, now);
        dashboard.Status.Should().Be(DashboardStatus.Active);
        dashboard.IsDeleted.Should().BeFalse();
    }
}
