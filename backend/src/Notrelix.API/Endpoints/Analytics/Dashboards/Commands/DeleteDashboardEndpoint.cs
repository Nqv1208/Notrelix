using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.DeleteDashboard;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Commands;

public static class DeleteDashboardEndpoint
{
    public static IEndpointRouteBuilder MapDeleteDashboard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/", HandleAsync)
            .WithName("Analytics.Dashboards.DeleteDashboard")
            .WithTags("Analytics")
            .WithSummary("Permanently delete a dashboard the caller owns (its widgets go with it; sources are untouched)")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid dashboardId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteDashboardCommand(workspaceId, dashboardId, expectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
