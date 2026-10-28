using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Reactions;

internal static class ReactionCommentLookup
{
    public const string BoardItemKind = "work-management.board-item";
    public const string CommentKind = "collaboration.comment";

    /// <summary>The live comment, required to sit on the authorized item so a comment id cannot borrow another item's permission.</summary>
    public static async Task<Comment> RequireOnItemAsync(ICollaborationDbContext context, Guid boardItemId, Guid commentId, CancellationToken ct)
    {
        var itemKind = ResourceKind.Create(BoardItemKind);
        return await context.Comments.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == commentId && c.Target.Kind == itemKind && c.Target.ResourceId == boardItemId && c.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(Comment), commentId);
    }
}
