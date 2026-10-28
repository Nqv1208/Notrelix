using Notrelix.API.Extensions;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Collaboration.Reactions.Commands.RemoveReaction;

namespace Notrelix.API.Endpoints.Collaboration.Comments.Commands;

public static class ReactionEndpoints
{
    public static IEndpointRouteBuilder MapBoardItemCommentReactions(this IEndpointRouteBuilder group)
    {
        group.MapResourcePut("/{commentId:guid}/reactions/{emoji}", AddAsync)
            .WithName("Collaboration.Comments.AddReaction")
            .WithTags("Collaboration.Comments")
            .WithSummary("React to a comment on a board item");
        group.MapResourceDelete("/{commentId:guid}/reactions/{emoji}", RemoveAsync)
            .WithName("Collaboration.Comments.RemoveReaction")
            .WithTags("Collaboration.Comments")
            .WithSummary("Withdraw the caller's reaction to a comment on a board item");
        return group;
    }

    private static async Task<IResult> AddAsync(Guid boardItemId, Guid commentId, string emoji, ISender sender)
    {
        var result = await sender.Send(new AddReactionCommand(boardItemId, commentId, emoji));
        return result.ToNoContentResult();
    }

    private static async Task<IResult> RemoveAsync(Guid boardItemId, Guid commentId, string emoji, ISender sender)
    {
        var result = await sender.Send(new RemoveReactionCommand(boardItemId, commentId, emoji));
        return result.ToNoContentResult();
    }
}
