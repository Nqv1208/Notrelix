using Notrelix.API.Contracts.WorkManagement.Approvals.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.DecideApproval;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class DecideApprovalEndpoint
{
    public static IEndpointRouteBuilder MapDecideApproval(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/{requestId:guid}/decision", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Approvals.Decide")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Approve or reject an approval request as its named approver")
            .WithDescription("Only the person named on a step of the request may decide it, and only once: a second decision is a conflict. The decision is approve or reject; the comment is optional.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        Guid requestId,
        DecideApprovalRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DecideApprovalCommand(workspaceId, requestId, body.Decision?.Trim().ToLowerInvariant() ?? string.Empty, body.Comment),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
