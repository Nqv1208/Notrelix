using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Application.Features.Workspaces.Navigation.Services;

/// <summary>
/// Persistence port for a Workspace Member's personal navigation state
/// (recents and pins, WX-DEC-011). Implemented in Infrastructure; the request
/// data session owns SaveChanges and the transaction.
///
/// Every read and write is keyed by (workspace, user): a member only ever
/// reads or changes their own entries.
/// </summary>
public interface IWorkspaceNavigationStore
{
    /// <summary>
    /// Serializes navigation writes of one member in one Workspace for the rest of
    /// the active request transaction, so the recents trim and the pin cap read a
    /// count that a concurrent request cannot invalidate before commit.
    /// </summary>
    Task LockMemberNavigationAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    /// <summary>Loads the member's recents as tracked entities.</summary>
    Task<IReadOnlyList<RecentResource>> LoadRecentsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    void AddRecent(RecentResource recent);

    void RemoveRecents(IReadOnlyCollection<RecentResource> recents);

    /// <summary>Newest first, at most <paramref name="limit"/> entries.</summary>
    Task<IReadOnlyList<RecentResourceEntry>> ListRecentsAsync(Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken);

    Task<PinnedResource?> FindPinAsync(Guid workspaceId, Guid userId, string resourceKind, Guid resourceId, CancellationToken cancellationToken);

    Task<int> CountPinsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken);

    void AddPin(PinnedResource pin);

    void RemovePin(PinnedResource pin);

    /// <summary>Newest pin first, at most <paramref name="limit"/> entries.</summary>
    Task<IReadOnlyList<PinnedResourceEntry>> ListPinsAsync(Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken);
}

public sealed record RecentResourceEntry(string Kind, Guid ResourceId, DateTimeOffset LastOpenedAt);

public sealed record PinnedResourceEntry(string Kind, Guid ResourceId, DateTimeOffset PinnedAt);
