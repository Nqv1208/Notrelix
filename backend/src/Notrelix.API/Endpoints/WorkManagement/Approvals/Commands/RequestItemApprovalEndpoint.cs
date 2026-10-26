using Notrelix.API.Contracts.WorkManagement.Approvals.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RequestItemApproval;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class RequestItemApprovalEndpoint
{
    public static IEndpointRouteBuilder MapRequestItemApproval(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/approval-requests", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Approvals.RequestForItem")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Ask a person to approve a board item")
            .Produces<ApprovalRequestIdDto>(StatusCodes.Status200OK, "application/json");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        RequestItemApprovalRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RequestItemApprovalCommand(itemId, body.ApproverUserId, body.Note), cancellationToken);
        return result.ToApiResult();
    }
}
