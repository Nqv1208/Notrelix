using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListItemApprovalRequests;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Queries;

public static class ListItemApprovalRequestsEndpoint
{
    public static IEndpointRouteBuilder MapListItemApprovalRequests(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/approval-requests", HandleAsync)
            .WithName("WorkManagement.Approvals.ListForItem")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("List the approval requests asked about a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid itemId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListItemApprovalRequestsQuery(itemId), cancellationToken);
        return result.ToApiResult();
    }
}
