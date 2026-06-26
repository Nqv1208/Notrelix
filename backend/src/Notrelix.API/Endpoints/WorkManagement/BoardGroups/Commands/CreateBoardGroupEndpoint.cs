using Notrelix.API.Contracts.WorkManagement.BoardGroups.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class CreateBoardGroupEndpoint
{
    public static IEndpointRouteBuilder MapCreateBoardGroup(this IEndpointRouteBuilder group)
    {
        group.MapPost("/", HandleAsync)
            .WithName("WorkManagement.BoardGroups.Create")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Create a new group in board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        CreateBoardGroupRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateBoardGroupCommand(boardId, body.Title, body.Position?.ToString(), body.Color), cancellationToken);
        return result.ToCreatedResult();
    }
}

