using Notrelix.API.Contracts.Analytics.Dashboards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.MoveDashboardWidget;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Widgets;

public static class MoveDashboardWidgetEndpoint
{
    public static IEndpointRouteBuilder MapMoveDashboardWidget(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/position", HandleAsync)
            .WithName("Analytics.Dashboards.MoveDashboardWidget")
            .WithTags("Analytics")
            .WithSummary("Move or resize a widget of a dashboard the caller owns")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid dashboardId,
        Guid widgetId,
        MoveDashboardWidgetRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new MoveDashboardWidgetCommand(workspaceId, dashboardId, widgetId, request.X, request.Y, request.W, request.H, request.ExpectedVersion),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
