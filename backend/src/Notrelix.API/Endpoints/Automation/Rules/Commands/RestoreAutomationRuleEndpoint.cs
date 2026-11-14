using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Commands.RestoreAutomationRule;

namespace Notrelix.API.Endpoints.Automation.Rules.Commands;

public static class RestoreAutomationRuleEndpoint
{
    public static IEndpointRouteBuilder MapRestoreAutomationRule(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{automationId:guid}/restore", HandleAsync)
            .WithName("Automation.Rules.Restore")
            .WithTags("Automation.Rules")
            .WithSummary("Restore a deleted automation rule within 90 days: it comes back Disabled and takes no plan slot")
            .WithIdempotencyKey();
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid automationId, ISender sender, Guid? boardId = null)
    {
        var result = await sender.Send(new RestoreAutomationRuleCommand(automationId, boardId));
        return result.ToNoContentResult();
    }
}
