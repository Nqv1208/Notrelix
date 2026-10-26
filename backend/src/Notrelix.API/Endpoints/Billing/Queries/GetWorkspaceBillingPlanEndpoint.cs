using Notrelix.API.Extensions;
using Notrelix.Application.Features.Billing.Plans.Queries.GetWorkspaceBillingPlan;

namespace Notrelix.API.Endpoints.Billing.Queries;

public static class GetWorkspaceBillingPlanEndpoint
{
    public static IEndpointRouteBuilder MapGetWorkspaceBillingPlan(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/plan", HandleAsync)
            .WithName("Billing.Plan.Get")
            .WithTags("Billing")
            .WithSummary("Get the plan the workspace's account is on, the plan catalog and whether the caller may change the plan")
            .Produces<WorkspaceBillingPlanDto>();
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceBillingPlanQuery(workspaceId));
        return result.ToApiResult();
    }
}
