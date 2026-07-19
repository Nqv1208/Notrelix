using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Approvals.Commands.SoftDeleteApprovalRequest;

namespace Notrelix.API.Endpoints.WorkManagement.Approvals.Commands;

public static class SoftDeleteApprovalRequestEndpoint
{
    public static IEndpointRouteBuilder MapSoftDeleteApprovalRequest(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
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
            new SoftDeleteApprovalRequestCommand(requestId, 0),
            cancellationToken);
        return result.ToNoContentResult();
    }
}
