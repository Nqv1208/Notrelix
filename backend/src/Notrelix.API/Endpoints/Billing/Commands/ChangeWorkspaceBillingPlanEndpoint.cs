using Notrelix.API.Contracts.Billing;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Billing.Plans.Commands.ChangeAccountPlan;
using Notrelix.Application.Features.Billing.Plans.Queries.GetWorkspaceBillingPlan;

namespace Notrelix.API.Endpoints.Billing.Commands;

public static class ChangeWorkspaceBillingPlanEndpoint
{
    public static IEndpointRouteBuilder MapChangeWorkspaceBillingPlan(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePut("/plan", HandleAsync)
            .WithName("Billing.Plan.Change")
            .WithTags("Billing")
            .WithSummary("Move the workspace's account to another plan (account owner, development environments only)")
            .Produces<WorkspaceBillingPlanDto>();
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        ChangeWorkspaceBillingPlanRequest request,
        ISender sender)
    {
        var result = await sender.Send(new ChangeAccountPlanCommand(workspaceId, request.PlanCode));
        return result.ToApiResult();
    }
}
