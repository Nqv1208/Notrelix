using Notrelix.Application.Features.Collaboration.Public.ResourceSummary;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

namespace Notrelix.Infrastructure.CrossContext.WorkManagement.Collaboration;

/// <summary>
/// Consumer-side adapter for WorkManagement's collaboration counts port.
/// Maps board-item ids onto Collaboration's (kind, id) resource addressing and
/// reads through the Collaboration-owned resource-summary boundary; it never
/// touches Collaboration persistence directly. Missing ids map to zero counts.
/// </summary>
public sealed class WorkManagementCollaborationReadAdapter : IWorkManagementCollaborationReadPort
{
    private const string BoardItemKindValue = "work-management.board-item";

    private readonly ICollaborationResourceSummary _resourceSummary;

    public WorkManagementCollaborationReadAdapter(ICollaborationResourceSummary resourceSummary)
    {
        _resourceSummary = resourceSummary;
    }

    public async Task<IReadOnlyDictionary<Guid, WorkItemCollaborationCounts>> GetCountsAsync(
        IReadOnlyCollection<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        var counts = new Dictionary<Guid, WorkItemCollaborationCounts>(itemIds.Count);

        if (itemIds.Count == 0)
        {
            return counts;
        }

        var resources = itemIds
            .Distinct()
            .Select(itemId => (BoardItemKindValue, itemId))
            .ToArray();
        var summaries = await _resourceSummary.GetSummariesAsync(resources, cancellationToken);

        foreach (var itemId in itemIds)
        {
            var summary = summaries.GetValueOrDefault(itemId);
            counts[itemId] = summary is null
                ? new WorkItemCollaborationCounts(0, 0)
                : new WorkItemCollaborationCounts(summary.CommentCount, summary.AttachmentCount);
        }

        return counts;
    }
}