using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;

namespace Notrelix.API.Endpoints.Automation.Rules.Commands;

public static class CreateAutomationRuleEndpoint
{
    public static IEndpointRouteBuilder MapCreateAutomationRule(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithName("Automation.Rules.Create")
            .WithTags("Automation.Rules")
            .WithSummary("Create a workspace automation rule");
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
            request.ActionType,
            request.Configuration ?? "{}"));

        return result.ToCreatedResult($"/api/v1/automations/{result.Data}");
    }
}

internal sealed record CreateAutomationRuleRequest(
    string Name,
    string TriggerEvent,
    string ActionType,
    string? Configuration);
