namespace Notrelix.Application.Features.Collaboration.Attachments.Services;

/// <summary>
/// The target kinds an attachment can hang under. Collaboration owns the attachment; the
/// target's own context decides who may see or change it (see the per-target use cases).
/// </summary>
public static class AttachmentTargets
{
    public static readonly ResourceKind BoardItem = ResourceKind.Create("work-management.board-item");
    public static readonly ResourceKind Page = ResourceKind.Create("documents.page");

    /// <summary>Per-target cap for the kind; board items and pages have independent limits.</summary>
    public static int MaxAttachments(ResourceKind kind, AttachmentUploadOptions options) =>
        kind == Page ? options.MaxAttachmentsPerPage : options.MaxAttachmentsPerItem;
}
