using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Abstractions;

namespace Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemVote;

/// <summary>The vote count on one Item and Vote field, and whether the caller has voted.</summary>
public record BoardItemVoteStateDto(int Count, bool Voted);

/// <summary>
/// Casts or withdraws the caller's own vote on an Item for a Vote field. One vote per person: voting again, or
/// withdrawing a vote that is not there, changes nothing. A vote is a reaction, so it needs the right to comment.
/// </summary>
[IdempotencyOperation("work-management.board-items.set-board-item-vote.v1")]
public record SetBoardItemVoteCommand(Guid BoardItemId, Guid FieldId, bool Voted)
    : ICommand<Result<BoardItemVoteStateDto>>, IWriteRequest, IAuthenticatedRequest, IResourceScopedRequest, IRequirePermission, IIdempotentRequest
{
    public PermissionAction Action => PermissionAction.CreateComment;
    public ResourceRef Resource => ResourceRef.Create(ResourceKind.Create("work-management.board-item"), BoardItemId);
}

public class SetBoardItemVoteCommandHandler(
    IWorkManagementDbContext context,
    ICurrentRequestContext requestContext,
    IDateTimeProvider clock,
    IBoardRealtimeNotifier? realtime = null) : IRequestHandler<SetBoardItemVoteCommand, Result<BoardItemVoteStateDto>>
{
    public async Task<Result<BoardItemVoteStateDto>> Handle(SetBoardItemVoteCommand request, CancellationToken ct)
    {
        var response = await HandleCore(request, ct);
        if (realtime is not null) await realtime.NotifyAsync(request, response, ct);
        return response;
    }

    private async Task<Result<BoardItemVoteStateDto>> HandleCore(SetBoardItemVoteCommand request, CancellationToken ct)
    {
        var item = await context.BoardItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.BoardItemId && i.DeletedAt == null, ct)
            ?? throw new NotFoundException(nameof(BoardItem), request.BoardItemId);
        if (item.IsArchived)
            throw new BusinessRuleException("An archived item cannot be voted on.");

        var isVoteField = await context.BoardFields.AsNoTracking().AnyAsync(f =>
            f.Id == request.FieldId && f.BoardId == item.BoardId && f.Type == FieldType.Vote && f.DeletedAt == null, ct);
        if (!isVoteField) throw new NotFoundException(nameof(BoardField), request.FieldId);

        var userId = requestContext.UserId;
        var mine = await context.BoardItemVotes
            .FirstOrDefaultAsync(v => v.ItemId == item.Id && v.FieldId == request.FieldId && v.UserId == userId, ct);

        if (request.Voted && mine is null)
            context.BoardItemVotes.Add(BoardItemVote.Create(item.AccountId, item.WorkspaceId, item.BoardId, item.Id, request.FieldId, userId, clock.UtcNow));
        else if (!request.Voted && mine is not null)
            context.BoardItemVotes.Remove(mine);

        var others = await context.BoardItemVotes.AsNoTracking()
            .CountAsync(v => v.ItemId == item.Id && v.FieldId == request.FieldId && v.UserId != userId, ct);
        return Result<BoardItemVoteStateDto>.Success(new BoardItemVoteStateDto(others + (request.Voted ? 1 : 0), request.Voted));
    }
}
