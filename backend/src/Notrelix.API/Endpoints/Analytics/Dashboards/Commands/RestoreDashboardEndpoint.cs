using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.RestoreDashboard;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Commands;

public static class RestoreDashboardEndpoint
{
    public static IEndpointRouteBuilder MapRestoreDashboard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/restore", HandleAsync)
            .WithName("Analytics.Dashboards.RestoreDashboard")
            .WithTags("Analytics")
            .WithSummary("Restore an archived dashboard the caller owns to Active")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid dashboardId,
        DashboardVersionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RestoreDashboardCommand(workspaceId, dashboardId, request.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
