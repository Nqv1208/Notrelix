using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.RenameDashboard;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Commands;

public static class RenameDashboardEndpoint
{
    public static IEndpointRouteBuilder MapRenameDashboard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/", HandleAsync)
            .WithName("Analytics.Dashboards.RenameDashboard")
            .WithTags("Analytics")
            .WithSummary("Rename a dashboard the caller owns")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid dashboardId,
        RenameDashboardRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RenameDashboardCommand(workspaceId, dashboardId, request.Name, request.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
