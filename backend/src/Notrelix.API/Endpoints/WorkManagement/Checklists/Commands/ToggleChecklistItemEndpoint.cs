using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.ToggleChecklistItem;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class ToggleChecklistItemEndpoint
{
    public static IEndpointRouteBuilder MapToggleChecklistItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/toggle", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Checklists.ToggleItem")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Toggle the completion status of a checklist item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        [FromQuery] long expectedVersion,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ToggleChecklistItemCommand(itemId, expectedVersion), cancellationToken);
        return result.ToNoContentResult();
    }
}
