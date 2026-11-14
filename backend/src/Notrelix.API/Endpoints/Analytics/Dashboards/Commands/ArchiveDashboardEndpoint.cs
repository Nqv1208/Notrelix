using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.ArchiveDashboard;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Commands;

public static class ArchiveDashboardEndpoint
{
    public static IEndpointRouteBuilder MapArchiveDashboard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/archive", HandleAsync)
            .WithName("Analytics.Dashboards.ArchiveDashboard")
            .WithTags("Analytics")
            .WithSummary("Archive a dashboard the caller owns (read only afterwards)")
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
        var result = await sender.Send(new ArchiveDashboardCommand(workspaceId, dashboardId, request.ExpectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
