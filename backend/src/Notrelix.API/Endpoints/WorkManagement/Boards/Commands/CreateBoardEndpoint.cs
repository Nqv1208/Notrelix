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
        // An unknown or numeric visibility name is a client error (WM-V3-API-001), never a 500
        // from Enum.Parse or an undefined enum value.
        BoardVisibility? visibility = null;
        if (body.Visibility is not null)
        {
            if (int.TryParse(body.Visibility, out _) || !Enum.TryParse<BoardVisibility>(body.Visibility, ignoreCase: true, out var parsed))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["visibility"] = [$"Visibility must be one of: {string.Join(", ", Enum.GetNames<BoardVisibility>())}."],
                });
            }

            visibility = parsed;
        }

        var result = await sender.Send(new CreateBoardInWorkspaceCommand(workspaceId, body.Title, body.Description, body.Background, visibility), cancellationToken);
        return result.ToCreatedResult();
    }
}

