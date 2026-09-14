using Notrelix.Application.Features.Analytics.Abstractions;
using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;

namespace Notrelix.Infrastructure.CrossContext.Analytics.WorkManagement;

/// <summary>
/// Delegate-only cross-context adapter: implements the Analytics-owned
/// projection-source port and delegates every call to the producer-owned
/// Public <see cref="IWorkItemProjectionSource"/> contract, which is
/// implemented by WorkManagement Application over the Work producer's own
/// persistence. This adapter never reaches Work persistence directly.
/// </summary>
public sealed class WorkItemProjectionSourceAdapter : IWorkItemProjectionSourceAdapter
{
    private readonly IWorkItemProjectionSource _source;

    public WorkItemProjectionSourceAdapter(IWorkItemProjectionSource source)
    {
        _source = source;
    }

    public Task<IReadOnlyList<WorkItemPlacementSnapshot>> GetWorkspacePlacementsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return _source.GetWorkspacePlacementsAsync(workspaceId, cancellationToken);
    }

    public Task<WorkItemPlacementSnapshot?> GetItemPlacementAsync(
        Guid workspaceId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        return _source.GetItemPlacementAsync(workspaceId, itemId, cancellationToken);
    }
}