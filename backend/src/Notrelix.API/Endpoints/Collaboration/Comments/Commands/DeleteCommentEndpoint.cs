using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.DeleteComment;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class DeleteCommentEndpoint
{
    public static IEndpointRouteBuilder MapDeleteComment(this IEndpointRouteBuilder group)
    {
        group.MapResourceDelete("/{commentId:guid}", HandleAsync)
            .WithName("Collaboration.Comments.Delete")
            .WithTags("Collaboration.Comments")
            .WithSummary("Delete a comment")
            .WithDescription("Soft-deletes the author's comment. Pass expectedVersion as a query parameter; 412 when it is stale.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, [FromQuery] long expectedVersion, ISender sender)
    {
        var result = await sender.Send(new DeleteCommentCommand(commentId, expectedVersion));
        return result.ToNoContentResult();
    }
}
