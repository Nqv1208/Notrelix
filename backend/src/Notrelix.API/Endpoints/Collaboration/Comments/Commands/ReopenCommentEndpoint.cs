using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ReopenComment;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class ReopenCommentEndpoint
{
    public static IEndpointRouteBuilder MapReopenComment(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{commentId:guid}/reopen", HandleAsync)
            .WithName("Collaboration.Comments.Reopen")
            .WithTags("Collaboration.Comments")
            .WithSummary("Reopen a resolved comment thread")
            .WithDescription("Pass expectedVersion as a query parameter; 412 when it is stale.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, [FromQuery] long expectedVersion, ISender sender)
    {
        var result = await sender.Send(new ReopenCommentCommand(commentId, expectedVersion));
        return result.ToNoContentResult();
    }
}
