using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Producer-owned implementation of the Work placement projection source.
/// Reads only the producer's own persistence and returns the minimal
/// placement snapshot a consumer-owned projection needs. Consumers never
/// reach this persistence directly.
/// </summary>
public sealed class WorkItemProjectionSourceService : IWorkItemProjectionSource
{
    private readonly IWorkManagementDbContext _context;

    public WorkItemProjectionSourceService(IWorkManagementDbContext context)
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