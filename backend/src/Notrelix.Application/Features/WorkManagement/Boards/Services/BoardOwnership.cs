using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;

namespace Notrelix.Application.Features.WorkManagement.Boards.Services;

/// <summary>
/// A board always has an Owner: its creator, until ownership is handed over by promoting someone else first.
/// The Owner role is the board's own access source, so a person who creates a Private board is never left
/// without access to it, and the last Owner cannot be removed or lowered.
/// </summary>
public static class BoardOwnership
{
    /// <summary>The creator of a new board is its Owner, whatever the visibility.</summary>
    public static void AddCreatorAsOwner(IWorkManagementDbContext context, Guid boardId, Guid creatorId, DateTimeOffset now) =>
        context.BoardMembers.Add(BoardMemberEntity.Create(boardId, creatorId, BoardRole.Owner, now));

    /// <summary>
    /// Making a board Private ends every Space or workspace source, so the person who does it stays on
    /// it as Owner when they hold no board role yet. Returns true when the Owner row was added.
    /// </summary>
    public static async Task<bool> EnsureActorStaysAsync(
        IWorkManagementDbContext context, Guid boardId, Guid actorId, DateTimeOffset now, CancellationToken ct)
    {
        var isMember = await context.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == actorId, ct);
        if (isMember)
        {
            return false;
        }

        context.BoardMembers.Add(BoardMemberEntity.Create(boardId, actorId, BoardRole.Owner, now));
        return true;
    }

    /// <summary>Removing or lowering the last Owner is refused: hand ownership over by promoting someone else first.</summary>
    public static async Task EnsureAnotherOwnerAsync(
        IWorkManagementDbContext context, Guid boardId, Guid leavingUserId, CancellationToken ct)
    {
        var another = await context.BoardMembers.AsNoTracking()
            .AnyAsync(m => m.BoardId == boardId && m.UserId != leavingUserId && m.Role == BoardRole.Owner, ct);
        if (!another)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Board_LastOwner,
                "A board needs an owner. Make someone else the owner first.");
        }
    }
}
