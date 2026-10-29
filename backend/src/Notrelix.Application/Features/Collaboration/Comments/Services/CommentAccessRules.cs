using Notrelix.Application.Features.Collaboration.Abstractions;

namespace Notrelix.Application.Features.Collaboration.Comments.Services;

/// <summary>
/// Who may change a comment. The pipeline only admits workspace members to the comment; these rules decide the
/// rest from the comment's target: no view access to it is the same NotFound as a missing comment.
/// </summary>
public static class CommentAccessRules
{
    public const string AuthorOnlyCode = "collaboration.comment_author_only";
    public const string ThreadStarterOrEditorCode = "collaboration.comment_thread_starter_or_editor_only";

    /// <summary>Editing and deleting belong to the author alone, and only while the author still sees the target.</summary>
    public static async Task RequireAuthorAsync(Comment comment, Guid actorId, ICommentTargetAccess access, CancellationToken ct)
    {
        await RequireViewAsync(comment, access, ct);
        if (comment.CreatedBy != actorId)
        {
            throw new ForbiddenException("Only the author can change this comment.", AuthorOnlyCode);
        }
    }

    /// <summary>Resolving and reopening belong to the thread starter or a person who may edit the target.</summary>
    public static async Task RequireThreadResolverAsync(
        Comment comment, Guid actorId, ICollaborationDbContext context, ICommentTargetAccess access, CancellationToken ct)
    {
        await RequireViewAsync(comment, access, ct);

        var starter = comment.CreatedBy;
        if (comment.ParentId is { } parentId)
        {
            starter = await context.Comments.AsNoTracking()
                .Where(c => c.Id == parentId)
                .Select(c => c.CreatedBy)
                .FirstOrDefaultAsync(ct);
        }

        if (starter == actorId || await access.HasAsync(comment.Target, CommentTargetRight.Edit, ct))
        {
            return;
        }

        throw new ForbiddenException("Only the thread starter or an editor can change this thread.", ThreadStarterOrEditorCode);
    }

    private static async Task RequireViewAsync(Comment comment, ICommentTargetAccess access, CancellationToken ct)
    {
        if (!await access.HasAsync(comment.Target, CommentTargetRight.View, ct))
        {
            throw new NotFoundException(nameof(Comment), comment.Id);
        }
    }
}
