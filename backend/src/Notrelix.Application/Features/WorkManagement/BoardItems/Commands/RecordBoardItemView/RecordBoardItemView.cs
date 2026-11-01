using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RecordBoardItemView;

/// <summary>
/// Records that the caller opened an Item ("Đã xem"). Only board members are recorded: a workspace guest is
/// skipped. A person's row is rewritten at most every <see cref="BoardItemView.Throttle"/>, so opening an Item stays cheap.
/// </summary>
public record RecordBoardItemViewCommand(Guid BoardItemId)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission
{
    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class RecordBoardItemViewCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IWorkspaceAccessResolver workspaceAccess,
    IBoardItemViews views,
    IDateTimeProvider clock) : IRequestHandler<RecordBoardItemViewCommand, Result>
{
    public async Task<Result> Handle(RecordBoardItemViewCommand request, CancellationToken ct)
    {
        var item = await context.BoardItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        var userId = requestContext.UserId;
        if ((await workspaceAccess.ResolveAsync(item.WorkspaceId, userId, ct)).IsGuest) return Result.Success();

        await views.RecordAsync(item.AccountId, item.WorkspaceId, item.BoardId, item.Id, userId, clock.UtcNow, ct);
        return Result.Success();
    }
}
