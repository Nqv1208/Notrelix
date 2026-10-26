using Notrelix.API.Extensions;
using Notrelix.Application.Features.Analytics.Reports;
using Notrelix.Application.Features.Analytics.Reports.Queries.GetWorkspaceReport;

namespace Notrelix.API.Endpoints.Analytics.Queries;

public static class GetWorkspaceReportEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceReport(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/", HandleAsync)
            .WithName("Analytics.Reports.GetWorkspaceReport")
            .WithTags("Analytics")
            .WithSummary("Get the work report across the boards the caller can read, optionally narrowed to a department space")
            .Produces<WorkspaceReportDto>();
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid? spaceId,
        int? weeks,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetWorkspaceReportQuery(workspaceId, spaceId, weeks ?? WorkReportPeriod.DefaultWeeks),
            cancellationToken);
        return result.ToApiResult();
    }
}
