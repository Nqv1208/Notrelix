using Notrelix.Application.Features.Collaboration.Comments.Services;

namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// Who may delete an attachment: its uploader or an editor of its target. No view access to the target is
/// the same NotFound as a missing attachment.
/// </summary>
public static class AttachmentAccessRules
{
    public const string UploaderOrEditorCode = "collaboration.attachment_uploader_or_editor_only";

    public static async Task RequireDeleterAsync(Attachment attachment, Guid actorId, ICommentTargetAccess access, CancellationToken ct)
    {
        if (!await access.HasAsync(attachment.Target, CommentTargetRight.View, ct))
        {
            throw new NotFoundException(nameof(Attachment), attachment.Id);
        }

        if (attachment.CreatedBy == actorId || await access.HasAsync(attachment.Target, CommentTargetRight.Edit, ct))
        {
            return;
        }

        throw new ForbiddenException("Only the uploader or an editor can delete this attachment.", UploaderOrEditorCode);
    }
}
