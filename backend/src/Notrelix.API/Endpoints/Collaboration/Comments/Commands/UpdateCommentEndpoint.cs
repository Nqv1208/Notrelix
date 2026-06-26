using Notrelix.API.Contracts.Collaboration.Comments.Requests;
using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Comments.Commands.UpdateComment;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class UpdateCommentEndpoint
{
    public static IEndpointRouteBuilder MapUpdateComment(this IEndpointRouteBuilder group)
    {
        group.MapPatch("/{commentId:guid}", HandleAsync)
            .WithName("Collaboration.Comments.Update")
            .WithTags("Collaboration.Comments")
            .WithSummary("Update a comment");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, UpdateCommentRequest body, ISender sender)
    {
        var result = await sender.Send(new UpdateCommentCommand(commentId, body.ContentMd));
        return result.ToApiResult();
    }
}

