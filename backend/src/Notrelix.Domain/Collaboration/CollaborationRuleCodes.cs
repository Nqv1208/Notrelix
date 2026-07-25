namespace Notrelix.Domain.Collaboration;

/// <summary>
/// Rule codes for the Collaboration bounded context.
/// </summary>
public static class CollaborationRuleCodes
{
    // ── Notification ──────────────────────────────────────────────────────
    public const string Collaboration_Notification_CannotNotifySelf = "Collaboration_Notification_CannotNotifySelf";

    // ── Reaction ──────────────────────────────────────────────────────────
    public const string Collaboration_Reaction_DuplicateReaction = "Collaboration_Reaction_DuplicateReaction";

    // ── Comment ───────────────────────────────────────────────────────────
    public const string Collaboration_Comment_ParentNotFound = "Collaboration_Comment_ParentNotFound";
    public const string Collaboration_Comment_ParentMustBeInSameTarget = "Collaboration_Comment_ParentMustBeInSameTarget";

    // ── Attachment ────────────────────────────────────────────────────────
    public const string Collaboration_Attachment_FileSizeMustBePositive = "Collaboration_Attachment_FileSizeMustBePositive";
    public const string Collaboration_Attachment_MaxAttachmentsExceeded = "Collaboration_Attachment_MaxAttachmentsExceeded";
    public const string Collaboration_Attachment_FileSizeExceeded = "Collaboration_Attachment_FileSizeExceeded";
}
