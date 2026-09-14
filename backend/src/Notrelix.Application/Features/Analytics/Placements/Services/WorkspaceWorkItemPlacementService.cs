using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.Analytics.Projections.WorkItemPlacement;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;

namespace Notrelix.Application.Features.Analytics.Placements.Services;

/// <summary>
/// Analytics-owned placement projection maintenance. Event consumers and the
/// rebuild use case delegate here so live updates and rebuilds converge on the
/// same derived-state semantics: a single producer-timestamp watermark
/// (LastOccurredAt ticks). Live facts apply only when strictly newer; rebuild
/// snapshots may repair at an equal watermark but never overwrite a newer live
/// fact. Rows the rebuild snapshot lacks are revalidated against the producer
/// before deletion so a fact that arrived after the snapshot was taken cannot
/// be dropped.
/// </summary>
public sealed class WorkspaceWorkItemPlacementService
{
    private readonly IReportingDbContext _context;
    private readonly IWorkItemProjectionSourceAdapter _source;

    public WorkspaceWorkItemPlacementService(
        IReportingDbContext context,
        IWorkItemProjectionSourceAdapter source)
    {
        _context = context;
        _source = source;
    }

    /// <summary>
    /// Applies a Work placement fact at the producer timestamp watermark.
    /// Returns false when the fact is stale or a duplicate delivery
    /// (watermark not newer than the projection state).
    /// </summary>
    public async Task<bool> ApplyPlacementAsync(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid boardId,
        Guid groupId,
        bool isArchived,
        DateTimeOffset lastOccurredAt,
        CancellationToken cancellationToken)
    {
        var existing = await _context.WorkspaceWorkItemPlacements
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId, cancellationToken);

        if (existing is null)
        {
            _context.WorkspaceWorkItemPlacements.Add(WorkspaceWorkItemPlacementProjection.Upsert(
                accountId, workspaceId, itemId, boardId, groupId, isArchived, lastOccurredAt));
            return true;
        }

        return existing.ApplyNewer(boardId, groupId, isArchived, lastOccurredAt);
    }

    /// <summary>
    /// Marks the item archived in the projection, retaining its last known
    /// placement. Returns false when the fact is stale or duplicate.
    /// </summary>
    public async Task<bool> MarkArchivedAsync(
        Guid workspaceId,
        Guid itemId,
        DateTimeOffset lastOccurredAt,
        CancellationToken cancellationToken)
    {
        var existing = await _context.WorkspaceWorkItemPlacements
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId, cancellationToken);

        if (existing is null)
            return false;

        return existing.ApplyNewer(
            existing.BoardId,
            existing.GroupId,
            isArchived: true,
            lastOccurredAt);
    }

    /// <summary>
    /// Rebuild path: reconciles the Workspace's projection rows against the
    /// producer-owned snapshot. Existing rows are only rewritten when the
    /// snapshot watermark is at or ahead of local state; rows the snapshot
    /// lacks are revalidated through the producer item lookup and removed only
    /// when the producer no longer reports the item at all.
    /// </summary>
    public async Task RebuildWorkspaceAsync(
        Guid workspaceId,
        IReadOnlyList<WorkItemPlacementSnapshot> snapshot,
        CancellationToken cancellationToken)
    {
        var existing = await _context.WorkspaceWorkItemPlacements
            .Where(p => p.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        var byItem = snapshot.ToDictionary(s => s.ItemId);

        foreach (var row in existing)
        {
            if (byItem.TryGetValue(row.ItemId, out var source))
            {
                row.Reconcile(source.BoardId, source.GroupId, source.IsArchived, source.LastOccurredAt);
                continue;
            }

            // The row arrived (or drifted in) after the snapshot was taken —
            // deletion requires proof the producer no longer has the item.
            var revalidated = await _source.GetItemPlacementAsync(
                workspaceId, row.ItemId, cancellationToken);

            if (revalidated is null)
                _context.WorkspaceWorkItemPlacements.Remove(row);
            else
                row.Reconcile(revalidated.BoardId, revalidated.GroupId, revalidated.IsArchived, revalidated.LastOccurredAt);
        }

        var knownIds = existing.Select(p => p.ItemId).ToHashSet();
        foreach (var source in snapshot)
        {
            if (knownIds.Contains(source.ItemId))
                continue;

            _context.WorkspaceWorkItemPlacements.Add(WorkspaceWorkItemPlacementProjection.Upsert(
                source.AccountId, workspaceId, source.ItemId, source.BoardId, source.GroupId, source.IsArchived,
                source.LastOccurredAt));
        }
    }
}
