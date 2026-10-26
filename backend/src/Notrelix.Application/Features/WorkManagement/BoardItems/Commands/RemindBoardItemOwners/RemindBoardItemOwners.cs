using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RemindBoardItemOwners;

/// <summary>
/// "Nhắc": the caller, who waits on an Item, asks each person in charge of it for an update. Everyone assigned
/// except the caller is told in their Inbox, naming the caller (Collaboration reacts to the recorded fact). The caller may do this once per Item per 24
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
    IBoardItemReminders reminders) : IRequestHandler<RemindBoardItemOwnersCommand, Result<RemindBoardItemOwnersResult>>
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
        // One atomic step decides the 24 hour limit: of simultaneous presses exactly one claims it and the rest
        // lose without telling anyone. The claim and the fact recorded below (which reaches Collaboration through
        // the outbox, to be turned into Inbox entries) share the request transaction.
        var claim = await reminders.TryClaimAsync(
            item.AccountId, item.WorkspaceId, item.BoardId, item.Id, senderId, now, cancellationToken);
        if (!claim.Claimed)
        {
            throw new ConflictException(
                $"The people in charge were already reminded; it is possible again at {claim.NextRemindAt:O}.",
                RemindBoardItemOwnersCommand.TooSoonErrorCode);
        }

        item.RecordOwnersReminded(senderId, recipients, now);

        return Result<RemindBoardItemOwnersResult>.Success(new(now, claim.NextRemindAt, recipients.Count));
    }
}

public class RemindBoardItemOwnersCommandValidator : AbstractValidator<RemindBoardItemOwnersCommand>
{
    public RemindBoardItemOwnersCommandValidator() => RuleFor(x => x.BoardItemId).NotEmpty();
}
