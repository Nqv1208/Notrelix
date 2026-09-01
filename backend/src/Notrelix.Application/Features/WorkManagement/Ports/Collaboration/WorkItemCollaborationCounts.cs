namespace Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

/// <summary>
/// Aggregated collaboration counts for a set of work-management board items.
/// Returned by <see cref="IWorkManagementCollaborationReadPort"/>.
/// </summary>
public sealed record WorkItemCollaborationCounts(
    int CommentCount,
    int AttachmentCount);
