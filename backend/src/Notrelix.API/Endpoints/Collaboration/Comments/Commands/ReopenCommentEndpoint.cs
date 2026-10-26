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
            .WithSummary("Reopen a resolved comment thread");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, ISender sender)
    {
        var result = await sender.Send(new ReopenCommentCommand(commentId));
        return result.ToNoContentResult();
    }
}
