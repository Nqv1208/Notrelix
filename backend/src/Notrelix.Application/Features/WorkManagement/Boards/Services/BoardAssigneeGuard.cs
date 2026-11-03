using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.Boards.Services;

/// <summary>
/// A board Viewer (read-only role) acts on nothing, so they are never given work: assigning an Item or a
/// checklist step to them, or naming them in a person field, is refused with the reason and nothing changes.
/// </summary>
public static class BoardAssigneeGuard
{
    /// <summary>The 403 reason when the assignee cannot yet see the board: the caller may share the board first (WM-040).</summary>
    public const string BoardNotSharedCode = "work-management.assignee.board-not-shared";

    public static async Task EnsureNotViewerAsync(
        IWorkManagementDbContext context, Guid boardId, Guid userId, CancellationToken ct)
    {
        var isViewer = await context.BoardMembers.AsNoTracking()
            .AnyAsync(m => m.BoardId == boardId && m.UserId == userId && BoardRoles.Viewers.Contains(m.Role), ct);
        if (isViewer)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_BoardItem_AssigneeIsViewer,
                "This person is a viewer of the board and cannot be given work. Make them a member first.");
        }
    }

    /// <summary>The same refusal for a set of people, in one query.</summary>
    public static async Task EnsureNoneViewerAsync(
        IWorkManagementDbContext context, Guid boardId, IReadOnlyCollection<Guid> userIds, CancellationToken ct)
    {
        var anyViewer = await context.BoardMembers.AsNoTracking()
            .AnyAsync(m => m.BoardId == boardId && userIds.Contains(m.UserId) && BoardRoles.Viewers.Contains(m.Role), ct);
        if (anyViewer)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_BoardItem_AssigneeIsViewer,
                "This person is a viewer of the board and cannot be given work. Make them a member first.");
        }
    }
}
