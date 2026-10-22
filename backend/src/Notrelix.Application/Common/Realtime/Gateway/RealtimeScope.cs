namespace Notrelix.Application.Common.Realtime.Gateway;

/// <summary>
/// The closed set of subscription scopes the realtime gateway can authorize.
/// A scope names exactly one backend stream; adding a kind requires a
/// matching authorization query owned by the context that owns the resource.
/// </summary>
public enum RealtimeScopeKind
{
    Workspace,
    Board,
}

/// <summary>A subscription target: a scope kind plus the resource id it names.</summary>
public readonly record struct RealtimeScope(RealtimeScopeKind Kind, Guid Id)
{
    public const string WorkspaceWireKind = "workspace";
    public const string BoardWireKind = "board";

    public string WireKind => Kind switch
    {
        RealtimeScopeKind.Workspace => WorkspaceWireKind,
        RealtimeScopeKind.Board => BoardWireKind,
        _ => throw new InvalidOperationException($"Unsupported realtime scope kind {Kind}."),
    };

    /// <summary>The stream topic this scope receives; one scope maps to one stream.</summary>
    public RealtimeTopic Topic => Kind switch
    {
        RealtimeScopeKind.Workspace => new RealtimeTopic("workspace", "Workspace", Id),
        RealtimeScopeKind.Board => new RealtimeTopic("board", "Board", Id),
        _ => throw new InvalidOperationException($"Unsupported realtime scope kind {Kind}."),
    };

    /// <summary>The stream key producers stamp on events for this scope.</summary>
    public string StreamKey => $"{Topic.Namespace}:{Topic.ResourceKind}:{Id:N}";

    public static bool TryParse(string? wireKind, string? id, out RealtimeScope scope)
    {
        scope = default;
        if (!Guid.TryParse(id, out var parsed) || parsed == Guid.Empty)
        {
            return false;
        }

        switch (wireKind)
        {
            case WorkspaceWireKind:
                scope = new RealtimeScope(RealtimeScopeKind.Workspace, parsed);
                return true;
            case BoardWireKind:
                scope = new RealtimeScope(RealtimeScopeKind.Board, parsed);
                return true;
            default:
                return false;
        }
    }
}
