using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class ArchiveBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapArchiveBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/archive", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.Archive")
            .Produces<BoardItemVersionDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Archive a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        ArchiveBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ArchiveBoardItemCommand(itemId, body.ExpectedVersion), cancellationToken);
        return result.ToItemVersionResult();
    }
}
