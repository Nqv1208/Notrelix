using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.RestoreApprovalRequest;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class RestoreApprovalRequestEndpoint
{
    public static IEndpointRouteBuilder MapRestoreApprovalRequest(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/restore", HandleAsync)
            .WithName("WorkManagement.Approvals.Restore")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Restore a soft-deleted approval request");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RestoreApprovalRequestCommand(requestId),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
