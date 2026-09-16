using Notrelix.Application.Features.WorkManagement.Public.ItemPlacement;

namespace Notrelix.Application.Features.Analytics.Abstractions;

/// <summary>
/// Analytics-owned runtime adapter seam over the producer-owned placement
/// facts. Live consumers resolve the current placement of one Work item when
/// a fact payload lacks account/group scope; the rebuild use case fetches the
/// full Workspace placement snapshot. The adapter consumer never touches Work
/// persistence: Infrastructure wires this port to a delegate adapter that
/// reaches the producer-owned Public <see cref="IWorkItemProjectionSource"/>
/// contract.
/// </summary>
public interface IWorkItemProjectionSourceAdapter
{
    Task<IReadOnlyList<WorkItemPlacementSnapshot>> GetWorkspacePlacementsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);

    Task<WorkItemPlacementSnapshot?> GetItemPlacementAsync(
        Guid workspaceId,
        Guid itemId,
        CancellationToken cancellationToken);
}