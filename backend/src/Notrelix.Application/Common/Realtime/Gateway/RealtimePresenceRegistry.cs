namespace Notrelix.Application.Common.Realtime.Gateway;

/// <summary>
/// Who is online right now, as the realtime gateway of this process sees it: a connection that reports the "online" state counts, one that
/// reports idle or offline, expires or closes does not. It is memory only, never stored and never used to grant access.
/// </summary>
public interface IRealtimePresenceRegistry
{
    /// <summary>Records the state a connection last reported for a person in a Workspace.</summary>
    void Mark(Guid connectionId, Guid workspaceId, Guid userId, bool online);

    /// <summary>Forgets a closed connection.</summary>
    void Drop(Guid connectionId);

    /// <summary>The people with at least one online connection in the Workspace.</summary>
    IReadOnlySet<Guid> OnlineUserIds(Guid workspaceId);
}

/// <remarks>shortcut: one process only; a person connected to another instance counts as absent, so @here under-notifies; upgrade to a Redis-backed set when the gateway scales out.</remarks>
public sealed class RealtimePresenceRegistry : IRealtimePresenceRegistry
{
    private readonly Lock _gate = new();
    private readonly Dictionary<(Guid ConnectionId, Guid WorkspaceId), Guid> _online = new();

    public void Mark(Guid connectionId, Guid workspaceId, Guid userId, bool online)
    {
        lock (_gate)
        {
            if (online)
                _online[(connectionId, workspaceId)] = userId;
            else
                _online.Remove((connectionId, workspaceId));
        }
    }

    public void Drop(Guid connectionId)
    {
        lock (_gate)
        {
            foreach (var key in _online.Keys.Where(k => k.ConnectionId == connectionId).ToList())
                _online.Remove(key);
        }
    }

    public IReadOnlySet<Guid> OnlineUserIds(Guid workspaceId)
    {
        lock (_gate)
        {
            return _online.Where(e => e.Key.WorkspaceId == workspaceId).Select(e => e.Value).ToHashSet();
        }
    }
}
