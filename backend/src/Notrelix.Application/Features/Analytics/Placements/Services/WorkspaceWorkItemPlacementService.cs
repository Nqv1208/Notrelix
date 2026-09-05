using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Queries;
using Notrelix.Domain.Analytics.Placements;

namespace Notrelix.Application.Features.Analytics.Placements.Services;

/// <summary>
/// Analytics-owned placement projection maintenance. Event consumers and the
/// rebuild use case delegate here so live updates and rebuilds converge on the
/// same derived-state semantics: last-write-wins by producer revision.
/// </summary>
public sealed class WorkspaceWorkItemPlacementService
{
    private readonly IReportingDbContext _context;

    public WorkspaceWorkItemPlacementService(IReportingDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Applies a Work placement fact. Returns false when the fact is stale or a
    /// duplicate delivery (revision not newer than the projection state).
    /// </summary>
    public async Task<bool> ApplyPlacementAsync(
        Guid accountId,
        Guid workspaceId,
        Guid itemId,
        Guid boardId,
        Guid groupId,
        bool isArchived,
        long sourceRevision,
        DateTimeOffset lastOccurredAt,
        CancellationToken cancellationToken)
    {
        var existing = await _context.WorkspaceWorkItemPlacements
            .FirstOrDefaultAsync(p => p.WorkspaceId == workspaceId && p.ItemId == itemId, cancellationToken);

        if (existing is null)
        {
            _context.WorkspaceWorkItemPlacements.Add(WorkspaceWorkItemPlacementProjection.Upsert(
                accountId, workspaceId, itemId, boardId, groupId, isArchived, sourceRevision, lastOccurredAt));
            return true;
        }

        return existing.ApplyNewer(boardId, groupId, isArchived, sourceRevision, lastOccurredAt);
    }

    /// <summary>
    /// Marks the item archived in the projection, retaining its last known
    /// placement. Returns false when the fact is stale or duplicate.
    /// </summary>
    public async Task<bool> MarkArchivedAsync(
        Guid workspaceId,
        Guid itemId,
        long sourceRevision,
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
            sourceRevision,
            lastOccurredAt);
    }

    /// <summary>
    /// Rebuild path: replaces the Workspace's projection rows with the
    /// producer-owned snapshot. Removes rows for items no longer present.
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
                row.Reconcile(source.BoardId, source.GroupId, source.IsArchived, source.Revision, source.LastOccurredAt);
            }
            else
            {
                _context.WorkspaceWorkItemPlacements.Remove(row);
            }
        }

        var knownIds = existing.Select(p => p.ItemId).ToHashSet();
        foreach (var source in snapshot)
        {
            if (knownIds.Contains(source.ItemId))
                continue;

            _context.WorkspaceWorkItemPlacements.Add(WorkspaceWorkItemPlacementProjection.Upsert(
                source.AccountId, workspaceId, source.ItemId, source.BoardId, source.GroupId, source.IsArchived,
                source.Revision, source.LastOccurredAt));
        }
    }
}
