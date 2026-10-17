using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Navigation;

namespace Notrelix.Application.Tests.Features.Workspaces.Navigation;

/// <summary>
/// In-memory port double for handler tests. It mirrors the store contract
/// (per-member keyed reads, tracked writes) and records whether the member
/// navigation lock was taken before the state was read.
/// </summary>
internal sealed class InMemoryWorkspaceNavigationStore : IWorkspaceNavigationStore
{
    public List<RecentResource> Recents { get; } = new();
    public List<PinnedResource> Pins { get; } = new();
    public List<(Guid WorkspaceId, Guid UserId)> Locks { get; } = new();
    public bool ReadBeforeLock { get; private set; }

    public Task LockMemberNavigationAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        Locks.Add((workspaceId, userId));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RecentResource>> LoadRecentsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        TrackRead(workspaceId, userId);
        IReadOnlyList<RecentResource> result = Recents.Where(r => r.WorkspaceId == workspaceId && r.UserId == userId).ToList();
        return Task.FromResult(result);
    }

    public void AddRecent(RecentResource recent) => Recents.Add(recent);

    public void RemoveRecents(IReadOnlyCollection<RecentResource> recents)
    {
        foreach (var recent in recents)
            Recents.Remove(recent);
    }

    public Task<IReadOnlyList<RecentResourceEntry>> ListRecentsAsync(Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken)
    {
        IReadOnlyList<RecentResourceEntry> result = Recents
            .Where(r => r.WorkspaceId == workspaceId && r.UserId == userId)
            .OrderByDescending(r => r.LastOpenedAt)
            .ThenByDescending(r => r.Id)
            .Take(limit)
            .Select(r => new RecentResourceEntry(r.ResourceKind, r.ResourceId, r.LastOpenedAt))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<PinnedResource?> FindPinAsync(Guid workspaceId, Guid userId, string resourceKind, Guid resourceId, CancellationToken cancellationToken)
    {
        TrackRead(workspaceId, userId);
        return Task.FromResult(Pins.FirstOrDefault(p =>
            p.WorkspaceId == workspaceId && p.UserId == userId && p.ResourceKind == resourceKind && p.ResourceId == resourceId));
    }

    public Task<int> CountPinsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        TrackRead(workspaceId, userId);
        return Task.FromResult(Pins.Count(p => p.WorkspaceId == workspaceId && p.UserId == userId));
    }

    public void AddPin(PinnedResource pin) => Pins.Add(pin);

    public void RemovePin(PinnedResource pin) => Pins.Remove(pin);

    public Task<IReadOnlyList<PinnedResourceEntry>> ListPinsAsync(Guid workspaceId, Guid userId, int limit, CancellationToken cancellationToken)
    {
        IReadOnlyList<PinnedResourceEntry> result = Pins
            .Where(p => p.WorkspaceId == workspaceId && p.UserId == userId)
            .OrderByDescending(p => p.PinnedAt)
            .ThenByDescending(p => p.Id)
            .Take(limit)
            .Select(p => new PinnedResourceEntry(p.ResourceKind, p.ResourceId, p.PinnedAt))
            .ToList();
        return Task.FromResult(result);
    }

    private void TrackRead(Guid workspaceId, Guid userId)
    {
        if (!Locks.Contains((workspaceId, userId)))
            ReadBeforeLock = true;
    }
}
