namespace Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

/// <summary>
/// Cross-context read port (spec 5.1): WorkManagement queries read
/// Collaboration comment/attachment counts through this projection port
/// instead of injecting the Collaboration DbContext.
/// </summary>
public interface IWorkManagementCollaborationReadPort
{
    Task<IReadOnlyDictionary<Guid, WorkItemCollaborationCounts>> GetCountsAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// The board items of one Workspace the person follows, newest follow first, at most
    /// <paramref name="limit"/>. Following is Collaboration's; whether the person may still read an
    /// item is decided by the caller.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetFollowedItemIdsAsync(
        Guid workspaceId,
        Guid userId,
        int limit,
        CancellationToken cancellationToken);
}
