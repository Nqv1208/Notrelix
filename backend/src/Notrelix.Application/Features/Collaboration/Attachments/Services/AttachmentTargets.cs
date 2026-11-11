namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// The target kinds an attachment can hang under. Collaboration owns the attachment; the
/// target's own context decides who may see or change it (see the per-target use cases).
/// </summary>
public static class AttachmentTargets
{
    public static readonly ResourceKind BoardItem = ResourceKind.Create("work-management.board-item");
    public static readonly ResourceKind Page = ResourceKind.Create("documents.page");

    /// <summary>A file on a Chat message (CHT-012). Chat decides who may see or change it through the Collaboration port; Collaboration keeps the row and the file policy.</summary>
    public static readonly ResourceKind ChatMessage = ResourceKind.Create("chat.message");

    /// <summary>Per-target cap for the kind; board items, pages and chat messages have independent limits.</summary>
    public static int MaxAttachments(ResourceKind kind, AttachmentUploadOptions options) =>
        kind == Page ? options.MaxAttachmentsPerPage
        : kind == ChatMessage ? options.MaxAttachmentsPerMessage
        : options.MaxAttachmentsPerItem;
}
