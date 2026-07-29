using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Analytics.Dashboards;
using Notrelix.Domain.Analytics.Widgets;

namespace Notrelix.Domain.Tests.Analytics.Dashboards;

[CoversAggregate(typeof(Dashboard))]
public class DashboardWidgetScopeTests
{
    [CoversMutation(typeof(Dashboard), "AddWidget(System.String,Notrelix.Domain.Analytics.Dashboards.DashboardWidgetType,Notrelix.Domain.SharedKernel.JsonValue,Notrelix.Domain.Analytics.Widgets.WidgetPosition,System.Guid,System.DateTimeOffset)", MutationScenario.Scope)]
    [Fact]
    public void AddWidget_ShouldSetScopeMatchingDashboard()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var dashboard = Dashboard.Create(accountId, workspaceId, "Dashboard", Guid.NewGuid(), DateTimeOffset.UtcNow);

        dashboard.AddWidget("Widget", DashboardWidgetType.TextWidget, JsonValue.Create("""{"content":"test"}"""), WidgetPosition.Create(0, 0, 1, 1), Guid.NewGuid(), DateTimeOffset.UtcNow);

        var widget = dashboard.Widgets.Single();
        widget.AccountId.Should().Be(accountId);
        widget.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void MultipleWidgets_ShouldAllMatchDashboardScope()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var dashboard = Dashboard.Create(accountId, workspaceId, "Dashboard", Guid.NewGuid(), DateTimeOffset.UtcNow);

        dashboard.AddWidget("A", DashboardWidgetType.TextWidget, JsonValue.Create("""{"content":"a"}"""), WidgetPosition.Create(0, 0, 1, 1), Guid.NewGuid(), DateTimeOffset.UtcNow);
        dashboard.AddWidget("B", DashboardWidgetType.TextWidget, JsonValue.Create("""{"content":"b"}"""), WidgetPosition.Create(1, 0, 1, 1), Guid.NewGuid(), DateTimeOffset.UtcNow);

        dashboard.Widgets.Should().AllSatisfy(w =>
        {
            w.AccountId.Should().Be(accountId);
            w.WorkspaceId.Should().Be(workspaceId);
        });
    }

    [CoversMutation(typeof(Dashboard), "AddWidget(System.String,Notrelix.Domain.Analytics.Dashboards.DashboardWidgetType,Notrelix.Domain.SharedKernel.JsonValue,Notrelix.Domain.Analytics.Widgets.WidgetPosition,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void AddWidget_WithUnknownWidgetType_ShouldThrow()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => dashboard.AddWidget("Bad", (DashboardWidgetType)99, JsonValue.Create("""{"content":"test"}"""), WidgetPosition.Create(0, 0, 1, 1), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Unknown widget type*");
    }

    [CoversMutation(typeof(Dashboard), "AddWidget(System.String,Notrelix.Domain.Analytics.Dashboards.DashboardWidgetType,Notrelix.Domain.SharedKernel.JsonValue,Notrelix.Domain.Analytics.Widgets.WidgetPosition,System.Guid,System.DateTimeOffset)", MutationScenario.Valid)]
    [Fact]
    public void AddWidget_WithUnknownWidgetType_ShouldNotMutateDashboard()
    {
        var dashboard = Dashboard.Create(Guid.NewGuid(), Guid.NewGuid(), "Dashboard", Guid.NewGuid(), DateTimeOffset.UtcNow);
        var versionBefore = dashboard.Version;

        var act = () => dashboard.AddWidget("Bad", (DashboardWidgetType)99, JsonValue.Create("""{"content":"test"}"""), WidgetPosition.Create(0, 0, 1, 1), Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>();
        dashboard.Version.Should().Be(versionBefore);
        dashboard.Widgets.Should().BeEmpty();
    }
}
