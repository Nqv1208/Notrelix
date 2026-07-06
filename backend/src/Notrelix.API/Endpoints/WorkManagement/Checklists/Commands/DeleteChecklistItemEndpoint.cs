using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class DeleteChecklistItemEndpoint
{
    public static IEndpointRouteBuilder MapDeleteChecklistItem(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/", HandleAsync)
            .WithName("WorkManagement.Checklists.DeleteItem")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Delete a checklist item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeleteChecklistItemCommand(itemId), cancellationToken);
        return result.ToNoContentResult();
    }
}
