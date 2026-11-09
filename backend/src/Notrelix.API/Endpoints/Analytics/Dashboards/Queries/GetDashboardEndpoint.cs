using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.DTOs;
using Notrelix.Application.Features.Analytics.Dashboards.Queries.GetDashboard;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Queries;

public static class GetDashboardEndpoint
{
    public static IEndpointRouteBuilder MapGetDashboard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Analytics.Dashboards.GetDashboard")
            .WithTags("Analytics")
            .WithSummary("Open one dashboard with its widgets (the creator only)")
            .Produces<DashboardDetailDto>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid dashboardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetDashboardQuery(workspaceId, dashboardId), cancellationToken);
        return result.ToApiResult();
    }
}
