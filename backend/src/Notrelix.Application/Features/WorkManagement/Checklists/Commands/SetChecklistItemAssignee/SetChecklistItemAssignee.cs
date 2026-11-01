using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Boards.Services;

namespace Notrelix.Application.Features.WorkManagement.Checklists.Commands.SetChecklistItemAssignee;

/// <summary>
/// Gives one checklist item to a person ("Giao mục này"), or to nobody (null). Like assigning the Item itself,
/// it needs the assign right, and the person must be able to see the Board when the assignment is made.
/// </summary>
[IdempotencyOperation("work-management.checklists.set-checklist-item-assignee.v1")]
public record SetChecklistItemAssigneeCommand(Guid ItemId, Guid? AssigneeUserId, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.AssignItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.checklist-item"), ItemId);
}

public class SetChecklistItemAssigneeCommandHandler(
    IWorkManagementDbContext context,
    ICurrentUser currentUser,
    IWorkspaceAccessResolver workspaceAccess,
    IResourceVisibility visibility,
    IDateTimeProvider clock,
    IBoardRealtimeNotifier? realtime = null) : IRequestHandler<SetChecklistItemAssigneeCommand, Result>
{
    public async Task<Result> Handle(SetChecklistItemAssigneeCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (realtime is not null) await realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result> HandleCore(SetChecklistItemAssigneeCommand request, CancellationToken ct)
    {
        var checklist = await ChecklistAggregateLoader.LoadOwningAsync(context, request.ItemId, ct);

        if (request.AssigneeUserId is { } assignee)
        {
            var access = await workspaceAccess.ResolveAsync(checklist.WorkspaceId, assignee, ct);
            if (!access.CanAccess)
                throw new ForbiddenException("Chỉ có thể giao cho thành viên thuộc cùng workspace.");

            var boardId = await context.BoardItems.AsNoTracking()
                .Where(i => i.Id == checklist.ItemId)
                .Select(i => (Guid?)i.BoardId)
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException(nameof(BoardItem), checklist.ItemId);
            var visibleBoards = await visibility.VisibleBoardIdsAsync(checklist.WorkspaceId, assignee, ct);
            if (!visibleBoards.Contains(boardId))
                throw new ForbiddenException("Người này chưa xem được board của việc, nên chưa thể giao mục. Hãy chia sẻ board với họ trước.");
            await BoardAssigneeGuard.EnsureNotViewerAsync(context, boardId, assignee, ct);
        }

        checklist.SetItemAssignee(request.ItemId, request.AssigneeUserId, currentUser.UserId, clock.UtcNow);
        return Result.Success();
    }
}
