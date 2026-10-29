using Notrelix.Application.Features.Collaboration.Comments.Services;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// Who may delete an attachment: an editor of its target, the same right that uploads one. No view access
/// to the target is the same NotFound as a missing attachment.
/// </summary>
public static class AttachmentAccessRules
{
    public const string EditorOnlyCode = "collaboration.attachment_editor_only";

    public static async Task RequireDeleterAsync(Attachment attachment, ICommentTargetAccess access, CancellationToken ct)
    {
        if (!await access.HasAsync(attachment.Target, CommentTargetRight.View, ct))
        {
            throw new NotFoundException(nameof(Attachment), attachment.Id);
        }

        if (!await access.HasAsync(attachment.Target, CommentTargetRight.Edit, ct))
        {
            throw new ForbiddenException("Only an editor can delete this attachment.", EditorOnlyCode);
        }
    }
}
