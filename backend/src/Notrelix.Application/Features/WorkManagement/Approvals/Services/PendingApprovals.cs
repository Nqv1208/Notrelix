using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Approvals.Services;

/// <summary>An approval request waiting for one person, with the Item it is about.</summary>
public sealed record PendingApproval(
    Guid RequestId,
    Guid ItemId,
    Guid BoardId,
    string? ItemKey,
    string ItemName,
    Guid RequestedBy,
    DateTimeOffset RequestedAt,
    string? Note);

public static class PendingApprovals
{
    private const string BoardItemKind = "work-management.board-item";

    /// <summary>
    /// The pending approval requests that name <paramref name="userId"/> on a pending step, about live Items on the
    /// given readable Boards, newest first. Requests about anything else (a Board, an Item that is gone or on a
    /// Board the person cannot read) are not listed.
    /// </summary>
    public static async Task<IReadOnlyList<PendingApproval>> ListAsync(
        IWorkManagementDbContext context,
        Guid workspaceId,
        Guid userId,
        IReadOnlyCollection<Guid> visibleBoardIds,
        CancellationToken cancellationToken)
    {
        if (visibleBoardIds.Count == 0) return [];

        var kind = ResourceKind.Create(BoardItemKind);
        var rows = await (
                from request in context.ApprovalRequests.AsNoTracking()
                join item in context.BoardItems.AsNoTracking() on request.Target.ResourceId equals item.Id
                where request.WorkspaceId == workspaceId
                      && request.DeletedAt == null
                      && request.Status == ApprovalStatus.Pending
                      && request.Target.Kind == kind
                      && request.Steps.Any(s => s.ApproverUserId == userId && s.Status == ApprovalStatus.Pending)
                      && item.WorkspaceId == workspaceId
                      && item.DeletedAt == null
                      && visibleBoardIds.Contains(item.BoardId)
                orderby request.CreatedAt descending, request.Id
                select new
                {
                    request.Id,
                    ItemId = item.Id,
                    item.BoardId,
                    item.ItemKey,
                    item.Name,
                    request.RequestedByUserId,
                    request.CreatedAt,
                    request.Description,
                })
            .Take(200)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new PendingApproval(
                r.Id, r.ItemId, r.BoardId, r.ItemKey, r.Name, r.RequestedByUserId, r.CreatedAt, r.Description))
            .ToList();
    }
}
