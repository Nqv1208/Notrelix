namespace Notrelix.Application.Features.Collaboration.Activity;

/// <summary>
/// The stable types of the page activity rows Collaboration records from Documents facts (product rule
/// DCT-031: a short feed of who renamed, moved, trashed or restored the page; not an audit log).
/// </summary>
public static class PageActivityTypes
{
    public const string ResourceKind = "documents.page";
    public const string SubjectType = "Page";
    public const string SourceContext = "documents";

    public const string Renamed = "docs.page-renamed";
    public const string Moved = "docs.page-moved";
    public const string Trashed = "docs.page-trashed";
    public const string Restored = "docs.page-restored";
}
