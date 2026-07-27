using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListApprovalRequests;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Queries;

public static class ListApprovalRequestsEndpoint
{
    public static IEndpointRouteBuilder MapListApprovalRequests(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/", HandleAsync)
            .WithName("WorkManagement.Approvals.List")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("List approval requests for a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListApprovalRequestsQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
