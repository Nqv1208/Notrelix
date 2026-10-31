using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Domain.WorkManagement;
using BoardMemberEntity = global::Notrelix.Domain.WorkManagement.Boards.BoardMember;

namespace Notrelix.Application.Features.WorkManagement.Boards.Services;

/// <summary>
/// A board always has an Owner: its creator, until ownership is handed over by promoting someone else first.
/// The Owner role is the board's own access source, so a person who creates a Private board is never left
/// without access to it, and the last Owner cannot be removed or lowered. No path adds an Owner row implicitly
/// for someone else's board. Only a board Owner, or a workspace Owner or Admin, hands the Owner role out or takes
/// it away: a board Admin manages Members and Viewers but cannot make or unmake an Owner.
/// </summary>
public static class BoardOwnership
{
    /// <summary>The creator of a new board is its Owner, whatever the visibility.</summary>
    public static void AddCreatorAsOwner(IWorkManagementDbContext context, Guid boardId, Guid creatorId, DateTimeOffset now) =>
        context.BoardMembers.Add(BoardMemberEntity.Create(boardId, creatorId, BoardRole.Owner, now));

    /// <summary>
    /// A Private board is reached only through its own members and grants, so it must keep an Owner. When it
    /// has none, making it Private is refused with <see cref="WorkManagementRuleCodes.WorkManagement_Board_PrivateNeedsOwner"/>,
    /// unless the person who does it explicitly chooses to be its Owner and holds no board role yet: that row is
    /// their own deliberate act (never added implicitly, because it would turn indirect access, such as a Space level
    /// or a time-limited grant, into a durable Owner row). Returns true when the Owner row was added.
    /// </summary>
    public static async Task<bool> EnsureOwnerForPrivateAsync(
        IWorkManagementDbContext context, Guid boardId, Guid actorId, bool addActorAsOwner, bool actorMayAssignOwner, DateTimeOffset now, CancellationToken ct)
    {
        var hasOwner = await context.BoardMembers.AsNoTracking()
            .AnyAsync(m => m.BoardId == boardId && m.Role == BoardRole.Owner, ct);
        if (hasOwner)
        {
            return false;
        }

        var actorHasRole = await context.BoardMembers.AsNoTracking()
            .AnyAsync(m => m.BoardId == boardId && m.UserId == actorId, ct);
        if (!addActorAsOwner || actorHasRole)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Board_PrivateNeedsOwner,
                "This board has no owner. Add yourself as its owner, or ask a workspace owner or admin to assign one, before making it Private.");
        }

        if (!actorMayAssignOwner)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Board_OwnerChangeNeedsOwner,
                "Only a workspace owner or admin can make themselves the owner of a board with no owner.");
        }

        context.BoardMembers.Add(BoardMemberEntity.Create(boardId, actorId, BoardRole.Owner, now));
        return true;
    }

    /// <summary>
    /// Making someone an Owner, lowering an Owner or removing one is for a board Owner or a workspace Owner or Admin.
    /// Anyone else, a board Admin included, is refused with
    /// <see cref="WorkManagementRuleCodes.WorkManagement_Board_OwnerChangeNeedsOwner"/> and nothing changes.
    /// </summary>
    public static async Task EnsureMayChangeOwnershipAsync(
        IWorkManagementDbContext context, IWorkspaceAccessResolver workspaceAccess,
        Guid workspaceId, Guid boardId, Guid actorId, CancellationToken ct)
    {
        if (await ActorMayAssignOwnerAsync(context, workspaceAccess, workspaceId, boardId, actorId, ct))
        {
            return;
        }

        throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
            WorkManagementRuleCodes.WorkManagement_Board_OwnerChangeNeedsOwner,
            "Only a board owner or a workspace owner or admin can make someone an owner or change an owner.");
    }

    public static async Task<bool> ActorMayAssignOwnerAsync(
        IWorkManagementDbContext context, IWorkspaceAccessResolver workspaceAccess,
        Guid workspaceId, Guid boardId, Guid actorId, CancellationToken ct)
    {
        var actorIsOwner = await context.BoardMembers.AsNoTracking()
            .AnyAsync(m => m.BoardId == boardId && m.UserId == actorId && m.Role == BoardRole.Owner, ct);
        return actorIsOwner || (await workspaceAccess.ResolveAsync(workspaceId, actorId, ct)).IsOwnerOrAdmin;
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
