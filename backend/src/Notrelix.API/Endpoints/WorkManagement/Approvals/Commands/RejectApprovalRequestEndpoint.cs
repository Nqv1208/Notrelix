using Notrelix.API.Contracts.WorkManagement.Approvals.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RejectApprovalRequest;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class RejectApprovalRequestEndpoint
{
    public static IEndpointRouteBuilder MapRejectApprovalRequest(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/reject", HandleAsync)
            .WithName("WorkManagement.Approvals.Reject")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Reject an approval request");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid requestId,
        RejectApprovalRequestRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RejectApprovalRequestCommand(requestId, body.Note, body.ExpectedVersion),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
