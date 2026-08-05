using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.WorkManagement.Common.Abstractions;

namespace Notrelix.Infrastructure.Data.ReadPorts.Collaboration;

/// <summary>
/// Queries Comments and Attachments targeted at <c>work-management.board-item</c>
/// resources, groups by resource id and returns zero counts for missing ids.
/// Soft-deleted comments are excluded from the counts.
/// </summary>
public sealed class WorkManagementCollaborationReadPort : IWorkManagementCollaborationReadPort
{
    private static readonly ResourceKind BoardItemKind =
        ResourceKind.Create("work-management.board-item");

    private readonly ICollaborationDbContext _context;

    public WorkManagementCollaborationReadPort(ICollaborationDbContext context)
    {
        _context = context;
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

        var commentCounts = await _context.Comments
            .AsNoTracking()
            .Where(comment => comment.Target.Kind == BoardItemKind
                              && itemIds.Contains(comment.Target.ResourceId)
                              && comment.DeletedAt == null)
            .GroupBy(comment => comment.Target.ResourceId)
            .Select(group => new { BoardItemId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BoardItemId, item => item.Count, cancellationToken);

        var attachmentCounts = await _context.Attachments
            .AsNoTracking()
            .Where(attachment => attachment.Target.Kind == BoardItemKind
                                 && itemIds.Contains(attachment.Target.ResourceId))
            .GroupBy(attachment => attachment.Target.ResourceId)
            .Select(group => new { BoardItemId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.BoardItemId, item => item.Count, cancellationToken);

        foreach (var itemId in itemIds)
        {
            counts[itemId] = new WorkItemCollaborationCounts(
                commentCounts.GetValueOrDefault(itemId),
                attachmentCounts.GetValueOrDefault(itemId));
        }

        return counts;
    }
}
