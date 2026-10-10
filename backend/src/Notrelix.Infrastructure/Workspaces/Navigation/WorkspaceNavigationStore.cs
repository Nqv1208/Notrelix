using System.Buffers.Binary;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Infrastructure.Data;

namespace Notrelix.Infrastructure.Workspaces.Navigation;

/// <summary>
/// PostgreSQL implementation of <see cref="IWorkspaceNavigationStore"/>.
///
/// Writes are tracked on the request <see cref="ApplicationDbContext"/> and saved
/// by the request data session. <see cref="LockMemberNavigationAsync"/> takes a
/// transaction-scoped advisory lock (<c>pg_advisory_xact_lock</c>) keyed by
/// (workspace, user) on the already-open request transaction, so two concurrent
/// "record open" or "pin" requests of the same member are serialized: the second
/// re-reads after the first commits, which keeps the unique key, the 30-entry
/// recents trim and the 50-pin cap correct under concurrency. Different members
/// never contend; a hash collision only over-serializes.
/// </summary>
public sealed class WorkspaceNavigationStore : IWorkspaceNavigationStore
{
    private readonly ApplicationDbContext _context;

    public WorkspaceNavigationStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LockMemberNavigationAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "The workspace navigation lock must be acquired inside the active request transaction.");
        }

        var key = ComputeLockKey(workspaceId, userId);
        await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({key})", cancellationToken);
    }

    public async Task<IReadOnlyList<RecentResource>> LoadRecentsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return await _context.RecentResources
            .Where(r => r.WorkspaceId == workspaceId && r.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public void AddRecent(RecentResource recent) => _context.RecentResources.Add(recent);

    public void RemoveRecents(IReadOnlyCollection<RecentResource> recents) => _context.RecentResources.RemoveRange(recents);

    public async Task<IReadOnlyList<RecentResourceEntry>> ListRecentsAsync(
        Guid workspaceId,
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _context.RecentResources
            .AsNoTracking()
            .Where(r => r.WorkspaceId == workspaceId && r.UserId == userId)
            .OrderByDescending(r => r.LastOpenedAt)
            .ThenByDescending(r => r.Id)
            .Take(limit)
            .Select(r => new RecentResourceEntry(r.ResourceKind, r.ResourceId, r.LastOpenedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<PinnedResource?> FindPinAsync(
        Guid workspaceId,
        Guid userId,
        string resourceKind,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return await _context.PinnedResources
            .FirstOrDefaultAsync(
                p => p.WorkspaceId == workspaceId
                     && p.UserId == userId
                     && p.ResourceKind == resourceKind
                     && p.ResourceId == resourceId,
                cancellationToken);
    }

    public async Task<int> CountPinsAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken)
    {
        return await _context.PinnedResources
            .CountAsync(p => p.WorkspaceId == workspaceId && p.UserId == userId, cancellationToken);
    }

    public void AddPin(PinnedResource pin) => _context.PinnedResources.Add(pin);

    public void RemovePin(PinnedResource pin) => _context.PinnedResources.Remove(pin);

    public async Task<IReadOnlyList<PinnedResourceEntry>> ListPinsAsync(
        Guid workspaceId,
        Guid userId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _context.PinnedResources
            .AsNoTracking()
            .Where(p => p.WorkspaceId == workspaceId && p.UserId == userId)
            .OrderByDescending(p => p.PinnedAt)
            .ThenByDescending(p => p.Id)
            .Take(limit)
            .Select(p => new PinnedResourceEntry(p.ResourceKind, p.ResourceId, p.PinnedAt))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// SHA-256 of UTF-8 "workspaces.navigation:&lt;workspace-guid-N&gt;:&lt;user-guid-N&gt;";
    /// the first 8 bytes, read big-endian, are the signed 64-bit advisory lock key.
    /// </summary>
    public static long ComputeLockKey(Guid workspaceId, Guid userId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"workspaces.navigation:{workspaceId:N}:{userId:N}"));
        return BinaryPrimitives.ReadInt64BigEndian(hash);
    }
}
