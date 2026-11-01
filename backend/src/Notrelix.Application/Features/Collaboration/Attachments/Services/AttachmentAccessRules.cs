using Notrelix.Application.Features.Collaboration.Comments.Services;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// Who may delete an attachment: an editor of its target, the same right that uploads one. No view access
/// to the target is the same NotFound as a missing attachment.
/// </summary>
public static class AttachmentAccessRules
{
    public const string EditorOnlyCode = "collaboration.attachment_editor_only";

    /// <summary>The same NotFound the pipeline gives a missing resource of the request, so a hidden one cannot be told apart.</summary>
    public static NotFoundException NotFound(ResourceRef requested) =>
        new(requested.Kind.ToString(), requested.ResourceId);

    public static async Task RequireDeleterAsync(Attachment attachment, ResourceRef requested, ICommentTargetAccess access, CancellationToken ct)
    {
        if (!await access.HasAsync(attachment.Target, CommentTargetRight.View, ct))
        {
            throw NotFound(requested);
        }

        if (!await access.HasAsync(attachment.Target, CommentTargetRight.Edit, ct))
        {
            throw new ForbiddenException("Only an editor can delete this attachment.", EditorOnlyCode);
        }
    }
}
