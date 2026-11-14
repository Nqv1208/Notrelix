using Notrelix.API.Contracts.Collaboration.Comments.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class UpdateCommentEndpoint
{
    public static IEndpointRouteBuilder MapUpdateComment(this IEndpointRouteBuilder group)
    {
        group.MapResourcePatch("/{commentId:guid}", HandleAsync)
            .WithName("Collaboration.Comments.Update")
            .WithTags("Collaboration.Comments")
            .WithSummary("Update a comment")
            .WithDescription("Replaces the text of the author's comment. Answers 412 with the current comment when expectedVersion is stale.")
            .ProducesProblems(StatusCodes.Status400BadRequest, StatusCodes.Status403Forbidden, StatusCodes.Status404NotFound, StatusCodes.Status412PreconditionFailed);
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, UpdateCommentRequest body, ISender sender)
    {
        if (body.ExpectedVersion is not { } expectedVersion)
            return EndpointExtensions.InvalidInput("expectedVersion is required.");

        var result = await sender.Send(new UpdateCommentCommand(commentId, body.ContentMd, expectedVersion));
        return result.ToApiResult();
    }
}

