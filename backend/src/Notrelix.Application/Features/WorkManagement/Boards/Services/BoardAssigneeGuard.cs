using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement;

namespace Notrelix.Application.Features.WorkManagement.Boards.Services;

/// <summary>
/// A board Viewer (read-only role) acts on nothing, so they are never given work: assigning an Item or a
/// checklist step to them, or naming them in a person field, is refused with the reason and nothing changes.
/// </summary>
public static class BoardAssigneeGuard
{
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
}
