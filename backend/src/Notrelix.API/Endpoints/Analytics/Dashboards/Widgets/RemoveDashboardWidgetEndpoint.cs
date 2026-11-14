using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Dashboards.Commands.RemoveDashboardWidget;

namespace Notrelix.API.Endpoints.Analytics.Dashboards.Widgets;

public static class RemoveDashboardWidgetEndpoint
{
    public static IEndpointRouteBuilder MapRemoveDashboardWidget(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("Analytics.Dashboards.RemoveDashboardWidget")
            .WithTags("Analytics")
            .WithSummary("Remove a widget from a dashboard the caller owns (its source is untouched)")
            .Produces(StatusCodes.Status204NoContent);
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid dashboardId,
        Guid widgetId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveDashboardWidgetCommand(workspaceId, dashboardId, widgetId, expectedVersion),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
