using Notrelix.API.Contracts.Automation.Rules.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Services;

namespace Notrelix.API.Endpoints.Automation.Rules.Commands;

public static class CreateAutomationRuleEndpoint
{
    public static IEndpointRouteBuilder MapCreateAutomationRule(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithName("Automation.Rules.Create")
            .WithTags("Automation.Rules")
            .WithSummary("Create a workspace automation rule")
            .Produces<Guid>(StatusCodes.Status201Created)
            .WithIdempotencyKey();
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        CreateAutomationRuleRequest request,
        ISender sender)
    {
        var result = await sender.Send(new CreateAutomationRuleCommand(
            workspaceId,
            request.Name,
            request.TriggerEvent,
            request.ActionType ?? request.Actions?.FirstOrDefault()?.Type ?? string.Empty,
            request.Configuration ?? "{}",
            request.BoardId,
            request.TriggerConfiguration,
            request.Actions?.Select(a => new RuleActionInput(a.Type, a.Configuration)).ToList(),
            request.Conditions));

        return result.ToCreatedResult($"/api/v1/automations/{result.Data}");
    }
}
