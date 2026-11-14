using Notrelix.Application.Features.Analytics.Dashboards.Commands.ArchiveDashboard;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.DeleteDashboard;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.RestoreDashboard;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Analytics;

public class DashboardLifecycleCommandTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("0196a1b2-0000-7000-8000-000000000002");
    private static readonly Guid DashboardId = Guid.Parse("0196a1b2-0000-7000-8000-000000000003");

    [Fact]
    public void Archive_restore_and_delete_declare_the_dashboard_as_the_version_resource()
    {
        var archive = new ArchiveDashboardCommand(WorkspaceId, DashboardId, 3);
        var restore = new RestoreDashboardCommand(WorkspaceId, DashboardId, 3);
        var delete = new DeleteDashboardCommand(WorkspaceId, DashboardId, 3);

        foreach (var request in new IExpectedVersionRequest[] { archive, restore, delete })
        {
            request.Resource.Kind.Value.Should().Be("analytics.dashboard");
            request.Resource.ResourceId.Should().Be(DashboardId);
            request.ExpectedVersion.Should().Be(3);
        }
    }

    [Fact]
    public void Archive_restore_and_delete_are_workspace_member_actions_with_a_verified_email()
    {
        var commands = new object[]
        {
            new ArchiveDashboardCommand(WorkspaceId, DashboardId, 1),
            new RestoreDashboardCommand(WorkspaceId, DashboardId, 1),
            new DeleteDashboardCommand(WorkspaceId, DashboardId, 1),
        };

        foreach (var command in commands)
        {
            ((IRequirePermission)command).Action.Should().Be(PermissionAction.ViewWorkspace);
            command.Should().BeAssignableTo<IRequireVerifiedEmail>();
            command.Should().BeAssignableTo<IWorkspaceRequest>();
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_version_below_one_is_refused_by_every_lifecycle_validator(long version)
    {
        Assert.False(new ArchiveDashboardCommandValidator().Validate(new ArchiveDashboardCommand(WorkspaceId, DashboardId, version)).IsValid);
        Assert.False(new RestoreDashboardCommandValidator().Validate(new RestoreDashboardCommand(WorkspaceId, DashboardId, version)).IsValid);
        Assert.False(new DeleteDashboardCommandValidator().Validate(new DeleteDashboardCommand(WorkspaceId, DashboardId, version)).IsValid);
    }

    [Fact]
    public void Empty_ids_are_refused_and_a_version_of_one_is_accepted()
    {
        Assert.True(new ArchiveDashboardCommandValidator().Validate(new ArchiveDashboardCommand(WorkspaceId, DashboardId, 1)).IsValid);
        Assert.False(new ArchiveDashboardCommandValidator().Validate(new ArchiveDashboardCommand(Guid.Empty, DashboardId, 1)).IsValid);
        Assert.False(new RestoreDashboardCommandValidator().Validate(new RestoreDashboardCommand(WorkspaceId, Guid.Empty, 1)).IsValid);
        Assert.False(new DeleteDashboardCommandValidator().Validate(new DeleteDashboardCommand(Guid.Empty, DashboardId, 1)).IsValid);
    }
}
