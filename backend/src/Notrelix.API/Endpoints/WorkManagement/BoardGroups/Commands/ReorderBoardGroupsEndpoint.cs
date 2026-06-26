using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.ReorderBoardGroups;

namespace Notrelix.API.Endpoints.WorkManagement.BoardGroups.Commands;

public static class ReorderBoardGroupsEndpoint
{
    public static IEndpointRouteBuilder MapReorderBoardGroups(this IEndpointRouteBuilder group)
    {
        group.MapPost("/reorder", HandleAsync)
            .WithName("WorkManagement.BoardGroups.Reorder")
            .WithTags("WorkManagement.BoardGroups")
            .WithSummary("Reorder groups in a board");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ReorderBoardGroupsCommand command,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var cmd = command with { BoardId = boardId };
        var result = await sender.Send(cmd, cancellationToken);
        return result.ToNoContentResult();
    }
}
