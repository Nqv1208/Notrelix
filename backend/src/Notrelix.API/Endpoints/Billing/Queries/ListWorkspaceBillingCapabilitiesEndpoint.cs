using Notrelix.API.Extensions;
using Notrelix.Application.Features.Billing.Entitlements.Queries.GetWorkspaceBillingCapabilities;

namespace Notrelix.API.Endpoints.Billing.Queries;

public static class ListWorkspaceBillingCapabilitiesEndpoint
{
    public static IEndpointRouteBuilder MapListWorkspaceBillingCapabilities(this IEndpointRouteBuilder group)
    {
        group.MapWorkspaceGet("/capabilities", HandleAsync)
            .WithName("Billing.Capabilities.List")
            .WithTags("Billing")
            .WithSummary("Get the capabilities the workspace's billing grants, with limit and usage")
            .Produces<IReadOnlyList<BillingCapabilityDto>>();
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid workspaceId, ISender sender)
    {
        var result = await sender.Send(new GetWorkspaceBillingCapabilitiesQuery(workspaceId));
        return result.ToApiResult();
    }
}
