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
            .WithSummary("Resolve a comment thread");
        return group;
    }

    private static async Task<IResult> HandleAsync(Guid commentId, ISender sender)
    {
        var result = await sender.Send(new ResolveCommentCommand(commentId));
        return result.ToNoContentResult();
    }
}
