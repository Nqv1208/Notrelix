using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoardPeople;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Queries;

public static class GetBoardPeopleEndpoint
{
    public static IEndpointRouteBuilder MapGetBoardPeople(this IEndpointRouteBuilder group)
    {
        group.MapResourceGet("/people", HandleAsync)
            .WithName("WorkManagement.Boards.GetPeople")
            .Produces<List<BoardPersonDto>>(StatusCodes.Status200OK, "application/json")
            .WithTags("WorkManagement.Boards")
            .WithSummary("List the people who can see a board (board-scoped pickers for Guests)")
            .WithDescription("The people with a board role or an explicit grant on this board. Readable by anyone who can view the board, Guests included, so a Guest's assignee, Person-field and mention pickers list only people on the board. Names and avatars only; no email.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetBoardPeopleQuery(boardId), cancellationToken);
        return result.ToApiResult();
    }
}
