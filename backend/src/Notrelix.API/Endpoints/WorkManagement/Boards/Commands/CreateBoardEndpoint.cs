using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class CreateBoardEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoard(this IEndpointRouteBuilder group)
    {
        group.MapWorkspacePost("/", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.Create")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Create a new board in workspace");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid workspaceId,
        CreateBoardInWorkspaceRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBoardInWorkspaceCommand(workspaceId, body.Title, body.Description, body.Background, body.Visibility is not null ? Enum.Parse<BoardVisibility>(body.Visibility, ignoreCase: true) : null), cancellationToken);
        return result.ToCreatedResult();
    }
}

