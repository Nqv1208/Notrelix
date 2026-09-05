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
}
