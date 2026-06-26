using Notrelix.API.Contracts.WorkManagement.BoardItems.Requests;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;

namespace Notrelix.API.Endpoints.WorkManagement.BoardItems.Commands;

public static class CreateBoardItemEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardItem(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("WorkManagement.BoardItems.Create")
            .WithTags("WorkManagement.BoardItems")
            .WithSummary("Create a new item in a board group");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        [FromHeader(Name = "X-Workspace-Id")] Guid workspaceId,
        CreateBoardItemRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBoardItemCommand(
            workspaceId,
            boardId,
            body.GroupId,
            body.Title,
            body.Position), cancellationToken);
        return Results.Ok(result);
    }
}

