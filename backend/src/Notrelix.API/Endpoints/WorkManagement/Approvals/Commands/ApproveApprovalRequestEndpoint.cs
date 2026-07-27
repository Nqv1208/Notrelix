using Notrelix.API.Contracts.WorkManagement.Approvals.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.ApproveApprovalRequest;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class ApproveApprovalRequestEndpoint
{
    public static IEndpointRouteBuilder MapApproveApprovalRequest(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/approve", HandleAsync)
            .WithName("WorkManagement.Approvals.Approve")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Approve an approval request");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid requestId,
        ApproveApprovalRequestRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ApproveApprovalRequestCommand(requestId, body.Note, body.ExpectedVersion),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
