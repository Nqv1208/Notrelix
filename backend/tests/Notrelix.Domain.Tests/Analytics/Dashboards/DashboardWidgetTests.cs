using FluentAssertions;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Tests.Analytics.Dashboards;

public class DashboardWidgetTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid WorkspaceId = Guid.NewGuid();
    private static readonly Guid DashboardId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        var position = WidgetPosition.Create(0, 0, 4, 3);
        var config = JsonValue.Create("{\"content\":\"Hello\"}");

        var widget = DashboardWidget.Create(AccountId, WorkspaceId, DashboardId, "Greeting", DashboardWidgetType.TextWidget, config, position);

        widget.AccountId.Should().Be(AccountId);
        widget.WorkspaceId.Should().Be(WorkspaceId);
        widget.DashboardId.Should().Be(DashboardId);
        widget.Title.Should().Be("Greeting");
        widget.Type.Should().Be(DashboardWidgetType.TextWidget);
        widget.Config.Should().Be(config);
        widget.Position.Should().Be(position);
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => DashboardWidget.Create(Guid.Empty, WorkspaceId, DashboardId, "Title", DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 1, 1));

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyWorkspaceId_ShouldThrow()
    {
        var act = () => DashboardWidget.Create(AccountId, Guid.Empty, DashboardId, "Title", DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 1, 1));

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyDashboardId_ShouldThrow()
    {
        var act = () => DashboardWidget.Create(AccountId, WorkspaceId, Guid.Empty, "Title", DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 1, 1));

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullTitle_ShouldThrow()
    {
        var act = () => DashboardWidget.Create(AccountId, WorkspaceId, DashboardId, null!, DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 1, 1));

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithInvalidConfig_ShouldThrow()
    {
        var act = () => DashboardWidget.Create(AccountId, WorkspaceId, DashboardId, "Title", DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"wrong\":\"field\"}"), WidgetPosition.Create(0, 0, 1, 1));

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Invalid widget config*");
    }

    [Fact]
    public void Create_WithNegativePosition_ShouldThrow()
    {
        var act = () => DashboardWidget.Create(AccountId, WorkspaceId, DashboardId, "Title", DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(-1, 0, 1, 1));

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*non-negative*");
    }

    [Fact]
    public void Create_WithZeroDimension_ShouldThrow()
    {
        var act = () => DashboardWidget.Create(AccountId, WorkspaceId, DashboardId, "Title", DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"content\":\"test\"}"), WidgetPosition.Create(0, 0, 0, 1));

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public void UpdatePosition_ShouldChangePosition()
    {
        var widget = CreateWidget();
        var newPosition = WidgetPosition.Create(2, 2, 6, 4);

        var changed = widget.UpdatePosition(newPosition);

        changed.Should().BeTrue();
        widget.Position.Should().Be(newPosition);
    }

    [Fact]
    public void UpdatePosition_ToSamePosition_ShouldReturnFalse()
    {
        var position = WidgetPosition.Create(0, 0, 4, 3);
        var widget = CreateWidget(position);

        var changed = widget.UpdatePosition(position);

        changed.Should().BeFalse();
    }

    [Fact]
    public void UpdatePosition_InvalidPosition_ShouldThrow()
    {
        var widget = CreateWidget();

        var act = () => widget.UpdatePosition(WidgetPosition.Create(-1, -1, 1, 1));

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void UpdateTitle_ShouldChangeTitle()
    {
        var widget = CreateWidget();

        var changed = widget.UpdateTitle("New Title");

        changed.Should().BeTrue();
        widget.Title.Should().Be("New Title");
    }

    [Fact]
    public void UpdateTitle_ShouldTrimWhitespace()
    {
        var widget = CreateWidget();

        widget.UpdateTitle("  Trimmed Title  ");

        widget.Title.Should().Be("Trimmed Title");
    }

    [Fact]
    public void UpdateTitle_ToSameTitle_ShouldReturnFalse()
    {
        var widget = CreateWidget(title: "Same Title");

        var changed = widget.UpdateTitle("Same Title");

        changed.Should().BeFalse();
    }

    [Fact]
    public void UpdateTitle_WithNull_ShouldThrow()
    {
        var widget = CreateWidget();

        var act = () => widget.UpdateTitle(null!);

        act.Should().Throw<BusinessRuleException>();
    }

    private static DashboardWidget CreateWidget(WidgetPosition? position = null, string? title = null)
    {
        return DashboardWidget.Create(
            AccountId, WorkspaceId, DashboardId,
            title ?? "Test Widget",
            DashboardWidgetType.TextWidget,
            JsonValue.Create("{\"content\":\"test\"}"),
            position ?? WidgetPosition.Create(0, 0, 4, 3));
    }
}
