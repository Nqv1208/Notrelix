using Notrelix.API.Contracts.Automation.Rules.Requests;
using Notrelix.API.Contracts.Automation.Rules.Responses;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;

namespace Notrelix.API.Endpoints.Automation.Rules.Commands;

public static class SetAutomationRuleEnabledEndpoint
{
    public static IEndpointRouteBuilder MapSetAutomationRuleEnabled(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePatch("/{automationId:guid}/enabled", HandleAsync)
            .WithName("Automation.Rules.SetEnabled")
            .WithTags("Automation.Rules")
            .WithSummary("Enable or disable an automation rule")
            .Produces<AutomationEnableRefusalResponse>(StatusCodes.Status400BadRequest, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid automationId,
        SetAutomationRuleEnabledRequest request,
        ISender sender)
    {
        var result = await sender.Send(new SetAutomationRuleEnabledCommand(automationId, request.IsEnabled, request.BoardId));
        return result.ToNoContentResult();
    }
}
