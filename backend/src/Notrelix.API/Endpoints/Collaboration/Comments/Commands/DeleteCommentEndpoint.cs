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
            .WithSummary("Delete a comment");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, ISender sender)
    {
        var result = await sender.Send(new DeleteCommentCommand(commentId));
        return result.ToNoContentResult();
    }
}
