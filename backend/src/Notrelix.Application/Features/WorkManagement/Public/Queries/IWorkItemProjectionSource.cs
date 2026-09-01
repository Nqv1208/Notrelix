namespace Notrelix.Application.Features.WorkManagement.Public.Queries;

/// <summary>
/// Producer-owned minimal placement snapshot for one Work item. Returns only
/// the current placement facts a consumer-owned projection needs; never
/// Domain aggregates or persistence types.
/// </summary>
public sealed record WorkItemPlacementSnapshot(
    Guid AccountId,
    Guid ItemId,
    Guid BoardId,
    Guid GroupId,
    bool IsArchived,
    long Revision,
    DateTimeOffset LastOccurredAt);

/// <summary>
/// Producer-owned rebuild source for consumer-owned Work placement
/// projections. Rebuild is a producer snapshot read — not outbox replay and
/// not foreign table access.
/// </summary>
public interface IWorkItemProjectionSource
{
    Task<IReadOnlyList<WorkItemPlacementSnapshot>> GetWorkspacePlacementsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken);
}
