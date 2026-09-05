using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.Queries;
using Notrelix.Infrastructure.Messaging.Consumers.Analytics;

namespace Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement;

/// <summary>
/// Infrastructure adapter exposing the producer-owned Work placement snapshot
/// to the Analytics rebuild path and created-item reconciliation.
/// </summary>
public sealed class WorkItemProjectionSourceAdapter : IWorkItemProjectionSource, IWorkItemProjectionSourceAdapter
{
    private readonly IWorkManagementDbContext _context;

    public WorkItemProjectionSourceAdapter(IWorkManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkItemPlacementSnapshot>> GetWorkspacePlacementsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var snapshots = await _context.BoardItems
            .AsNoTracking()
            .Where(item => item.WorkspaceId == workspaceId)
            .Select(item => new WorkItemPlacementSnapshot(
                item.AccountId,
                item.Id,
                item.BoardId,
                item.GroupId,
                item.IsArchived,
                item.Version,
                item.UpdatedAt ?? item.CreatedAt))
            .ToListAsync(cancellationToken);

        return snapshots;
    }

    public async Task<WorkItemPlacementSnapshot?> GetItemPlacementAsync(
        Guid workspaceId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _context.BoardItems
            .AsNoTracking()
            .Where(item => item.WorkspaceId == workspaceId && item.Id == itemId)
            .Select(item => new WorkItemPlacementSnapshot(
                item.AccountId,
                item.Id,
                item.BoardId,
                item.GroupId,
                item.IsArchived,
                item.Version,
                item.UpdatedAt ?? item.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return snapshot;
    }
}
