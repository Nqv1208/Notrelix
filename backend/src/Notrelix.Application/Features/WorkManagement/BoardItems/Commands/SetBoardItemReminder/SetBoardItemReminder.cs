using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemReminder;

/// <summary>
/// Sets how long before the due time assignees and followers are reminded: -1 off, 0 at the due time, 15, 60,
/// 1440 (the default), 2880 or 10080 minutes before. One reminder per Item.
/// </summary>
[IdempotencyOperation("work-management.board-items.set-board-item-reminder.v1")]
public record SetBoardItemReminderCommand(Guid BoardItemId, int OffsetMinutes, long ExpectedVersion)
    : ICommand<Result>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest, IExpectedVersionRequest
{
    public PermissionAction Action => PermissionAction.UpdateItem;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class SetBoardItemReminderCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock) : IRequestHandler<SetBoardItemReminderCommand, Result>
{
    public async Task<Result> Handle(SetBoardItemReminderCommand request, CancellationToken ct)
    {
        var item = await context.BoardItems.FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);

        item.SetReminder(request.OffsetMinutes, requestContext.UserId, clock.UtcNow);
        return Result.Success();
    }
}
