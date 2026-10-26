using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RemindBoardItemOwners;

/// <summary>
/// "Nhắc": the caller, who waits on an Item, asks each person in charge of it for an update. Everyone assigned
/// except the caller gets an Inbox notification naming the caller. The caller may do this once per Item per 24
/// hours (a durable rule of <see cref="BoardItemReminder"/>). Seeing the Item is enough to ask.
/// </summary>
[IdempotencyOperation("work-management.board-items.remind-owners.v1")]
public record RemindBoardItemOwnersCommand(Guid BoardItemId)
    : ICommand<Result<RemindBoardItemOwnersResult>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public const string TooSoonErrorCode = "work.item.remind-too-soon";

    public PermissionAction Action => PermissionAction.ViewBoard;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

/// <summary>When the reminder was sent, when the caller may send the next one, and how many people it reached.</summary>
public sealed record RemindBoardItemOwnersResult(DateTimeOffset RemindedAt, DateTimeOffset NextRemindAt, int Recipients);

public class RemindBoardItemOwnersCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IItemReminderNotifier notifier) : IRequestHandler<RemindBoardItemOwnersCommand, Result<RemindBoardItemOwnersResult>>
{
    public async Task<Result<RemindBoardItemOwnersResult>> Handle(RemindBoardItemOwnersCommand request, CancellationToken cancellationToken)
    {
        var item = await context.BoardItems.FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, cancellationToken);
        if (item is null) throw new NotFoundException(nameof(BoardItem), request.BoardItemId);
        if (item.IsArchived || item.CompletedAt is not null)
            throw new BusinessRuleException("A finished or archived item has nobody left to remind.");

        var senderId = requestContext.UserId;
        var recipients = await context.BoardItemMembers
            .AsNoTracking()
            .Where(m => m.ItemId == item.Id && m.UserId != senderId)
            .Select(m => m.UserId)
            .ToListAsync(cancellationToken);
        if (recipients.Count == 0)
            throw new BusinessRuleException("Nobody else is in charge of this item, so there is nobody to remind.");

        var now = clock.UtcNow;
        var reminder = await context.BoardItemReminders
            .FirstOrDefaultAsync(r => r.ItemId == item.Id && r.SenderUserId == senderId, cancellationToken);
        if (reminder is null)
        {
            reminder = BoardItemReminder.Create(item.AccountId, item.WorkspaceId, item.BoardId, item.Id, senderId, now);
            context.BoardItemReminders.Add(reminder);
        }
        else if (reminder.CanRemind(now))
        {
            reminder.Remind(now);
        }
        else
        {
            throw new ConflictException(
                $"The people in charge were already reminded; it is possible again at {reminder.NextRemindAt:O}.",
                RemindBoardItemOwnersCommand.TooSoonErrorCode);
        }

        // Same unit of work as the reminder row: the limit and the notification stand or fall together.
        await notifier.NotifyAsync(
            item.AccountId, item.WorkspaceId, item.Id, senderId, reminder.Id, now, recipients, cancellationToken);

        return Result<RemindBoardItemOwnersResult>.Success(new(now, reminder.NextRemindAt, recipients.Count));
    }
}

public class RemindBoardItemOwnersCommandValidator : AbstractValidator<RemindBoardItemOwnersCommand>
{
    public RemindBoardItemOwnersCommandValidator() => RuleFor(x => x.BoardItemId).NotEmpty();
}
