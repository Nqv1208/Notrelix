using Notrelix.API.Extensions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Analytics.Dashboards.DTOs;
using Notrelix.Application.Features.Analytics.Dashboards.Queries.ListDashboards;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Queries;

public static class ListDashboardsEndpoint
{
    public static IEndpointRouteBuilder MapListDashboards(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Analytics.Dashboards.ListDashboards")
            .WithTags("Analytics")
            .WithSummary("List the Active dashboards the caller may open in the Workspace, newest first")
            .WithDescription("Returns the caller's own Active dashboards as a cursor page: default 50, max 200 (limit). Pass nextCursor back as cursor for the next page. Archived and other members' private dashboards are not listed.")
            .Produces<CursorPage<DashboardSummaryDto>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status401Unauthorized, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        string? cursor,
        int? limit,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListDashboardsQuery(workspaceId, cursor, limit), cancellationToken);
        return result.ToApiResult();
    }
}
