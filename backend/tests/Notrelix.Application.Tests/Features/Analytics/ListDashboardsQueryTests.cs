using Notrelix.Application.Features.Analytics.Dashboards.Queries.ListDashboards;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Analytics;

public class ListDashboardsQueryTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("0196a1b2-0000-7000-8000-000000000002");

    [Fact]
    public void The_default_page_is_fifty_and_the_maximum_is_two_hundred()
    {
        Assert.Equal(50, ListDashboardsQuery.DefaultLimit);
        Assert.Equal(200, ListDashboardsQuery.MaxLimit);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(200)]
    public void A_limit_from_one_to_two_hundred_is_accepted(int limit)
    {
        Assert.True(new ListDashboardsQueryValidator().Validate(new ListDashboardsQuery(WorkspaceId, null, limit)).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void A_limit_outside_one_to_two_hundred_is_refused(int limit)
    {
        Assert.False(new ListDashboardsQueryValidator().Validate(new ListDashboardsQuery(WorkspaceId, null, limit)).IsValid);
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("!!!")]
    public void A_cursor_that_is_not_a_dashboard_cursor_is_refused(string cursor)
    {
        Assert.False(new ListDashboardsQueryValidator().Validate(new ListDashboardsQuery(WorkspaceId, cursor, null)).IsValid);
    }

    [Fact]
    public void Listing_is_a_workspace_member_read()
    {
        var query = new ListDashboardsQuery(WorkspaceId);

        Assert.IsAssignableFrom<IReadRequest>(query);
        Assert.Equal(PermissionAction.ViewWorkspace, ((IRequirePermission)query).Action);
    }
}
