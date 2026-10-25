using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateChildItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class CreateChildItemEndpoint
{
    public static IEndpointRouteBuilder MapCreateChildItem(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/child-items", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.BoardItems.CreateChild")
            .Produces<ChildItemDto>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Add a sub-item under a board item");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid itemId,
        CreateChildItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateChildItemCommand(itemId, body.Title), cancellationToken);
        return Results.Ok(result);
    }
}
