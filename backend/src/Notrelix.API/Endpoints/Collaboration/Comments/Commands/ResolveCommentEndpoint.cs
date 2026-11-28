using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.ResolveComment;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class ResolveCommentEndpoint
{
    public static IEndpointRouteBuilder MapResolveComment(this IEndpointRouteBuilder group)
    {
        group.MapResourcePost("/{commentId:guid}/resolve", HandleAsync)
            .WithName("Collaboration.Comments.Resolve")
            .WithTags("Collaboration.Comments")
            .WithSummary("Resolve a comment thread")
            .WithDescription("Pass expectedVersion as a query parameter; 412 when it is stale.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, [FromQuery] long expectedVersion, ISender sender)
    {
        var result = await sender.Send(new ResolveCommentCommand(commentId, expectedVersion));
        return result.ToNoContentResult();
    }
}
