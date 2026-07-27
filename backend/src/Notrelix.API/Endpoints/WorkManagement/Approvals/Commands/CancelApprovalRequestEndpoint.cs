using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.CancelApprovalRequest;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class CancelApprovalRequestEndpoint
{
    public static IEndpointRouteBuilder MapCancelApprovalRequest(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/cancel", HandleAsync)
            .WithName("WorkManagement.Approvals.Cancel")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Cancel an approval request");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelApprovalRequestCommand(requestId, 0),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
