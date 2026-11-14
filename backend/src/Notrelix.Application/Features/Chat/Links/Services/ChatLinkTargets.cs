namespace Notrelix.Application.Features.Chat.Links.Services;

/// <summary>
/// A link a message can carry (CHT-023): a Board item or a Page of the same Workspace, named by its ResourceRef. The client resolves a
/// pasted product URL to a reference, and the server accepts the URL as well, reading only the path and the item query of the product's
/// own routes (/workspaces/{id}/boards/{id}?item={id} and /workspaces/{id}/docs/{id}). The host is ignored: the reference is resolved in
/// the route's Workspace with the caller's own access, so a URL never reaches another Workspace or another site.
/// </summary>
public static class ChatLinkTargets
{
    public const string BoardItemKind = "work-management.board-item";
    public const string PageKind = "documents.page";

    /// <summary>
    /// A Board itself, named by a kind and an id (UC-CHT-H10). It is accepted by the resolver only as a ResourceRef (the preview a channel
    /// link shows); a product URL of a Board without an item is still not a message link.
    /// </summary>
    public const string BoardKind = "work-management.board";

    /// <summary>The longest URL the resolver accepts.</summary>
    public const int MaxUrlLength = 2048;

    public static readonly IReadOnlyList<string> SupportedKinds = [BoardItemKind, PageKind, BoardKind];

    public static bool IsSupported(string? kind) => kind is not null && SupportedKinds.Contains(kind, StringComparer.Ordinal);

    /// <summary>The reference a product URL of this Workspace names, or null for any other URL (another Workspace, another route, a Board without an item, another site).</summary>
    public static (string Kind, Guid Id)? FromUrl(string url, Guid workspaceId)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > MaxUrlLength)
            return null;

        var trimmed = url.Trim();
        string path;
        string query;
        if (trimmed.StartsWith('/') && !trimmed.StartsWith("//", StringComparison.Ordinal))
        {
            var withoutFragment = trimmed.Split('#')[0];
            var q = withoutFragment.IndexOf('?');
            path = q >= 0 ? withoutFragment[..q] : withoutFragment;
            query = q >= 0 ? withoutFragment[q..] : string.Empty;
        }
        else if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https")
        {
            path = absolute.AbsolutePath;
            query = absolute.Query;
        }
        else
        {
            return null;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 4
            || !string.Equals(segments[0], "workspaces", StringComparison.Ordinal)
            || !Guid.TryParse(segments[1], out var inWorkspace)
            || inWorkspace != workspaceId
            || !Guid.TryParse(segments[3], out var routeId))
        {
            return null;
        }

        if (string.Equals(segments[2], "docs", StringComparison.Ordinal))
            return (PageKind, routeId);

        if (string.Equals(segments[2], "boards", StringComparison.Ordinal))
        {
            var item = ItemFromQuery(query);
            return item is { } itemId ? (BoardItemKind, itemId) : null;
        }

        return null;
    }

    private static Guid? ItemFromQuery(string query)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq <= 0 || !string.Equals(pair[..eq], "item", StringComparison.Ordinal))
                continue;

            return Guid.TryParse(Uri.UnescapeDataString(pair[(eq + 1)..]), out var id) ? id : null;
        }

        return null;
    }
}
