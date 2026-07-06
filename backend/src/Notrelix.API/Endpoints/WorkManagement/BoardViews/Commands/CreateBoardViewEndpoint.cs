using Notrelix.API.Contracts.WorkManagement.BoardViews.Requests;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;

namespace Notrelix.API.Endpoints.WorkManagement.BoardViews.Commands;

public static class CreateBoardViewEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardView(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/", HandleAsync)
            .WithName("WorkManagement.BoardViews.Create")
            .WithTags("WorkManagement.BoardViews")
            .WithSummary("Create a new saved view config for a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateBoardViewRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBoardViewCommand(
            boardId,
            body.Name,
            body.ViewMode,
            "{}"), cancellationToken);
        return Results.Ok(result);
    }
}

