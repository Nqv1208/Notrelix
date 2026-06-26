using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Queries.GetWorkspaceAutomations;

namespace Notrelix.API.Endpoints.Automation.Rules.Queries;

public static class ListAutomationRulesEndpoint
{
    public static IEndpointRouteBuilder MapListAutomationRules(this IEndpointRouteBuilder group)
    {
        group.MapGet("/", HandleAsync)
            .WithName("Automation.Rules.List")
            .WithTags("Automation.Rules")
            .WithSummary("Get workspace automation rules");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceAutomationsQuery(workspaceId));
        return result.ToApiResult();
    }
}
