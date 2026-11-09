using Notrelix.API.Extensions;
using Notrelix.Application.Features.Automation.Executions.Commands.CancelAutomationExecution;

namespace Notrelix.API.Endpoints.Automation.Executions.Commands;

public static class CancelAutomationExecutionEndpoint
{
    public static IEndpointRouteBuilder MapCancelAutomationExecution(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{automationId:guid}/executions/{executionId:guid}/cancel", HandleAsync)
            .WithName("Automation.Executions.Cancel")
            .WithTags("Automation.Executions")
            .WithSummary("Cancel a queued automation execution: it ends Cancelled and no action runs; a running or finished one is refused")
            .WithIdempotencyKey();
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid automationId, Guid executionId, ISender sender, Guid? boardId = null)
    {
        var result = await sender.Send(new CancelAutomationExecutionCommand(automationId, executionId, boardId));
        return result.ToNoContentResult();
    }
}
