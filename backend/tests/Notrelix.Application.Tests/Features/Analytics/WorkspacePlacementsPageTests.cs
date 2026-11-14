using Notrelix.Application.Features.Analytics.Placements.Queries.GetWorkspacePlacements;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Analytics;

public class WorkspacePlacementsPageTests
{
    private static readonly Guid WorkspaceId = Guid.Parse("0196a1b2-0000-7000-8000-000000000001");

    [Fact]
    public void The_page_defaults_to_fifty_and_is_capped_at_two_hundred()
    {
        Assert.Equal(50, GetWorkspacePlacementsQuery.DefaultLimit);
        Assert.Equal(200, GetWorkspacePlacementsQuery.MaxLimit);
    }

    [Fact]
    public void The_list_needs_the_workspace_view_permission()
    {
        Assert.Equal(PermissionAction.ViewWorkspace, ((IRequirePermission)new GetWorkspacePlacementsQuery(WorkspaceId)).Action);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(201)]
    public void A_limit_outside_one_to_two_hundred_is_refused(int limit)
    {
        Assert.False(new GetWorkspacePlacementsQueryValidator()
            .Validate(new GetWorkspacePlacementsQuery(WorkspaceId, null, limit)).IsValid);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public void A_limit_within_the_bounds_is_accepted(int limit)
    {
        Assert.True(new GetWorkspacePlacementsQueryValidator()
            .Validate(new GetWorkspacePlacementsQuery(WorkspaceId, null, limit)).IsValid);
    }

    [Fact]
    public void A_cursor_that_is_not_a_placement_cursor_is_refused()
    {
        Assert.False(new GetWorkspacePlacementsQueryValidator()
            .Validate(new GetWorkspacePlacementsQuery(WorkspaceId, "not-a-cursor!", null)).IsValid);
    }

    [Fact]
    public void Without_a_cursor_or_limit_the_query_is_valid()
    {
        Assert.True(new GetWorkspacePlacementsQueryValidator()
            .Validate(new GetWorkspacePlacementsQuery(WorkspaceId)).IsValid);
    }
}
