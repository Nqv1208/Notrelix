using Notrelix.API.Contracts.Automation.Rules.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Commands.UpdateAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Services;

namespace Notrelix.API.Endpoints.Automation.Rules.Commands;

public static class UpdateAutomationRuleEndpoint
{
    public static IEndpointRouteBuilder MapUpdateAutomationRule(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/{automationId:guid}", HandleAsync)
            .WithName("Automation.Rules.Update")
            .WithTags("Automation.Rules")
            .WithSummary("Edit an automation rule (creates a new version)")
            .WithIdempotencyKey();
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid automationId,
        UpdateAutomationRuleRequest request,
        ISender sender)
    {
        var result = await sender.Send(new UpdateAutomationRuleCommand(
            automationId,
            request.Name,
            request.TriggerEvent,
            request.TriggerConfiguration,
            request.Actions.Select(a => new RuleActionInput(a.Type, a.Configuration)).ToList(),
            request.Conditions,
            request.BoardId));
        return result.ToNoContentResult();
    }
}
