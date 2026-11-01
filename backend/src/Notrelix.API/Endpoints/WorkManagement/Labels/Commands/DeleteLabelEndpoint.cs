using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;

namespace Notrelix.API.Endpoints.WorkManagement.Labels.Commands;

public static class DeleteLabelEndpoint
{
    public static IEndpointRouteBuilder MapDeleteLabel(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Labels.Delete")
            .WithTags("WorkManagement.Labels")
            .WithSummary("Delete a label");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid labelId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteLabelCommand(labelId, expectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
