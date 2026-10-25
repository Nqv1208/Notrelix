using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Rules.Commands.DeleteAutomationRule;

namespace Notrelix.API.Endpoints.Automation.Rules.Commands;

public static class DeleteAutomationRuleEndpoint
{
    public static IEndpointRouteBuilder MapDeleteAutomationRule(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceDelete("/{automationId:guid}", HandleAsync)
            .WithName("Automation.Rules.Delete")
            .WithTags("Automation.Rules")
            .WithSummary("Delete an automation rule: it is disabled, in-flight executions finish, its history stays readable for 90 days")
            .WithIdempotencyKey();
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid automationId, ISender sender, Guid? boardId = null)
    {
        var result = await sender.Send(new DeleteAutomationRuleCommand(automationId, boardId));
        return result.ToNoContentResult();
    }
}
