using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.DuplicateBoard;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class DuplicateBoardEndpoint
{
    public static IEndpointRouteBuilder MapDuplicateBoard(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/duplicate", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.Duplicate")
            .Produces<DuplicateBoardResult>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Copy a board into a new one, with its items when asked")
            .WithDescription("Copies settings, fields, groups, labels and views; root items only when includeItems is true (at most 500, otherwise refused). Members and automations are not copied. Assignees without access to the copy are dropped from the copied items. All or nothing; a retry with the same idempotency key creates one copy.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        DuplicateBoardRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DuplicateBoardCommand(boardId, body.Name, body.SpaceId, body.IncludeItems), cancellationToken);
        return result.ToApiResult();
    }
}
