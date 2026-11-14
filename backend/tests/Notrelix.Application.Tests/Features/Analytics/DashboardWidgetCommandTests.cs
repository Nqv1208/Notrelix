using Notrelix.Application.Features.Analytics.Dashboards.Commands.MoveDashboardWidget;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.RemoveDashboardWidget;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Analytics;

public class DashboardWidgetCommandTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("0196a1b2-0000-7000-8000-000000000002");
    private static readonly Guid DashboardId = Guid.Parse("0196a1b2-0000-7000-8000-000000000003");
    private static readonly Guid WidgetId = Guid.Parse("0196a1b2-0000-7000-8000-000000000004");

    [Fact]
    public void Move_and_remove_carry_the_expected_version_on_the_dashboard()
    {
        var move = new MoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 1, 2, 3, 4, 5);
        var remove = new RemoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 5);

        foreach (var request in new IExpectedVersionRequest[] { move, remove })
        {
            request.Resource.Kind.Value.Should().Be("analytics.dashboard");
            request.Resource.ResourceId.Should().Be(DashboardId);
            request.ExpectedVersion.Should().Be(5);
        }
    }

    [Fact]
    public void Move_and_remove_are_workspace_member_actions_with_a_verified_email()
    {
        var commands = new object[]
        {
            new MoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 0, 0, 1, 1, 1),
            new RemoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 1),
        };

        foreach (var command in commands)
        {
            ((IRequirePermission)command).Action.Should().Be(PermissionAction.ViewWorkspace);
            command.Should().BeAssignableTo<IRequireVerifiedEmail>();
            command.Should().BeAssignableTo<IWorkspaceRequest>();
        }
    }

    [Fact]
    public void Only_the_remove_is_idempotent_by_key()
    {
        var remove = new RemoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 1);

        Assert.IsAssignableFrom<IIdempotentRequest>(remove);
        Assert.False(typeof(IIdempotentRequest).IsAssignableFrom(typeof(MoveDashboardWidgetCommand)));
    }

    [Fact]
    public void Remove_declares_its_idempotency_operation()
    {
        var attribute = typeof(RemoveDashboardWidgetCommand).GetCustomAttributes(typeof(IdempotencyOperationAttribute), false)
            .Cast<IdempotencyOperationAttribute>()
            .Single();

        attribute.Operation.Should().Be("analytics.dashboard.remove-widget.v1");
    }

    [Fact]
    public void Empty_ids_and_a_version_below_one_are_refused()
    {
        Assert.True(new MoveDashboardWidgetCommandValidator().Validate(new MoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 0, 0, 1, 1, 1)).IsValid);
        Assert.False(new MoveDashboardWidgetCommandValidator().Validate(new MoveDashboardWidgetCommand(WorkspaceId, DashboardId, Guid.Empty, 0, 0, 1, 1, 1)).IsValid);
        Assert.False(new MoveDashboardWidgetCommandValidator().Validate(new MoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 0, 0, 1, 1, 0)).IsValid);
        Assert.True(new RemoveDashboardWidgetCommandValidator().Validate(new RemoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 1)).IsValid);
        Assert.False(new RemoveDashboardWidgetCommandValidator().Validate(new RemoveDashboardWidgetCommand(Guid.Empty, DashboardId, WidgetId, 1)).IsValid);
        Assert.False(new RemoveDashboardWidgetCommandValidator().Validate(new RemoveDashboardWidgetCommand(WorkspaceId, DashboardId, WidgetId, 0)).IsValid);
    }
}
