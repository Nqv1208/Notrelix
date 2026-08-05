using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.DeleteApprovalRequest;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class DeleteApprovalRequestEndpoint
{
    public static IEndpointRouteBuilder MapDeleteApprovalRequest(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Approvals.Delete")
            .WithTags("WorkManagement.Approvals")
            .WithSummary("Soft delete an approval request");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new DeleteApprovalRequestCommand(requestId, 0),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
