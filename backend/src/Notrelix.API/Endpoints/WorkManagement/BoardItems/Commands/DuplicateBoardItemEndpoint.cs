using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class DuplicateBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapDuplicateBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/duplicate", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Duplicate")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Duplicate a board item in its current group")
            .WithDescription("Optional body names the copy and chooses what it takes along (fields, checklists, sub-items, labels, assignees). Comments, history, files and links are never copied.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        DuplicateBoardItemRequest? body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DuplicateBoardItemCommand(
            itemId,
            body?.Name,
            body is null ? null : new DuplicateBoardItemOptions(body.IncludeFields, body.IncludeChecklists, body.IncludeSubItems, body.IncludeLabels, body.IncludeAssignees)), cancellationToken);
        return result.ToCreatedResult();
    }
}
