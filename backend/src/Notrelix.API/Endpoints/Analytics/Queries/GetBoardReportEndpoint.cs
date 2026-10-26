using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Reports;
using Notrelix.Application.Features.Analytics.Reports.Queries.GetBoardReport;

namespace Notrelix.API.Endpoints.Analytics.Queries;

public static class GetBoardReportEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardReport(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("Analytics.Reports.GetBoardReport")
            .WithTags("Analytics")
            .WithSummary("Get the work report of a board: items by status and assignee, overdue, completions per week and average completion time")
            .Produces<BoardReportDto>();
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        int? weeks,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetBoardReportQuery(boardId, weeks ?? WorkReportPeriod.DefaultWeeks),
            cancellationToken);
        return result.ToApiResult();
    }
}
