using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;

namespace Notrelix.API.Endpoints.Automation.Executions.Queries;

public static class ListAutomationExecutionsEndpoint
{
    public static IEndpointRouteBuilder MapListAutomationExecutions(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{automationId:guid}/executions", HandleAsync)
            .WithName("Automation.Executions.List")
            .WithTags("Automation.Executions")
            .WithSummary("Get automation execution history");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid automationId,
        ISender sender,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetAutomationExecutionsQuery(automationId, page, pageSize));
        return result.ToApiResult();
    }
}
