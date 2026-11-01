using Notrelix.API.Contracts.WorkManagement.Boards.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.MakeBoardPrivate;

namespace Notrelix.API.Endpoints.WorkManagement.Boards.Commands;

public static class MakeBoardPrivateEndpoint
{
    public static IEndpointRouteBuilder MapMakeBoardPrivate(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/sharing/private", HandleAsync)
            .WithIdempotencyKey()
            .WithName("WorkManagement.Boards.MakePrivate")
            .WithTags("WorkManagement.Boards")
            .WithSummary("Add people to a board, then make it Private, in one step")
            .WithDescription("Adds the given people as board members who can edit (people already on the board keep their role), then sets the visibility to Private, in one transaction. If any person cannot be added the board is unchanged.");
        return group;
    }

    private static async Task<IResult> HandleAsync(
        Guid boardId,
        MakeBoardPrivateRequest body,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MakeBoardPrivateCommand(boardId, body.AddUserIds ?? [], body.AddMeAsOwner), cancellationToken);
        return result.ToNoContentResult();
    }
}
