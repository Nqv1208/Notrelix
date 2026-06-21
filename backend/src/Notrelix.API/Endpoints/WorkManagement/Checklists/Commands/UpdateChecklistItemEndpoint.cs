using MediatR;
using Notrelix.API.Contracts.WorkManagement.Checklists.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;

namespace Notrelix.API.Endpoints.WorkManagement.Checklists.Commands;

public static class UpdateChecklistItemEndpoint
{
    public static IEndpointRouteBuilder MapUpdateChecklistItem(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/", HandleAsync)
            .WithName("WorkManagement.Checklists.UpdateItem")
            .WithTags("WorkManagement.Checklists")
            .WithSummary("Update a checklist item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        UpdateChecklistItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateChecklistItemCommand(itemId, body.IsChecked), cancellationToken);
        return result.ToApiResult();
    }
}

