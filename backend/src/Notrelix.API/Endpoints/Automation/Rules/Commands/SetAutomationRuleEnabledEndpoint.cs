using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;

namespace Notrelix.API.Endpoints.Automation.Rules.Commands;

public static class SetAutomationRuleEnabledEndpoint
{
    public static IEndpointRouteBuilder MapSetAutomationRuleEnabled(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/{automationId:guid}/enabled", HandleAsync)
            .WithName("Automation.Rules.SetEnabled")
            .WithTags("Automation.Rules")
            .WithSummary("Enable or disable an automation rule");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid automationId,
        SetAutomationRuleEnabledRequest request,
        ISender sender)
    {
        var result = await sender.Send(new SetAutomationRuleEnabledCommand(automationId, request.IsEnabled));
        return result.ToNoContentResult();
    }
}

internal sealed record SetAutomationRuleEnabledRequest(bool IsEnabled);
