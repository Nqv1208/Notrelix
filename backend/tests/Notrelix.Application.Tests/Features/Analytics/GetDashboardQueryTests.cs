using Notrelix.Application.Features.Analytics.Dashboards.Queries.GetDashboard;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Analytics;

public class GetDashboardQueryTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("0196a1b2-0000-7000-8000-000000000002");
    private static readonly Guid DashboardId = Guid.Parse("0196a1b2-0000-7000-8000-000000000003");

    [Fact]
    public void Opening_a_dashboard_is_a_workspace_member_read_for_one_id()
    {
        var query = new GetDashboardQuery(WorkspaceId, DashboardId);

        Assert.IsAssignableFrom<IReadRequest>(query);
        Assert.Equal(PermissionAction.ViewWorkspace, ((IRequirePermission)query).Action);
    }

    [Fact]
    public void Empty_ids_are_refused()
    {
        Assert.True(new GetDashboardQueryValidator().Validate(new GetDashboardQuery(WorkspaceId, DashboardId)).IsValid);
        Assert.False(new GetDashboardQueryValidator().Validate(new GetDashboardQuery(Guid.Empty, DashboardId)).IsValid);
        Assert.False(new GetDashboardQueryValidator().Validate(new GetDashboardQuery(WorkspaceId, Guid.Empty)).IsValid);
    }
}
