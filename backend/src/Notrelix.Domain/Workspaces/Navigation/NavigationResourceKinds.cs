namespace Notrelix.Domain.Workspaces.Navigation;

/// <summary>
/// Resource kinds a Workspace Member may keep in personal navigation state
/// (recents and pins, WX-DEC-011). Navigation stores a reference only
/// (kind + id); display names stay with the owning context.
/// </summary>
public static class NavigationResourceKinds
{
    public const string Board = "work-management.board";
    public const string Page = "documents.page";

    private static readonly HashSet<string> Supported = new(StringComparer.Ordinal) { Board, Page };

    public static IReadOnlyCollection<string> All => Supported;

    public static bool IsSupported(string? kind) => kind is not null && Supported.Contains(kind);
}
