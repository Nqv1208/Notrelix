using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Application.Features.WorkManagement.Boards.Services;

/// <summary>
/// A Board always keeps someone who manages it: removing or lowering the last person or Team at Manager
/// level is refused with the reason, and nothing changes. The Workspace Owner and Admins are not entries
/// of the Board and do not count; the Board's own access list does.
/// </summary>
public static class BoardManagerGuard
{
    public static async Task EnsureAnotherManagerAsync(
        IBoardSharingSummary sharing, Guid boardId, Guid leavingUserId, CancellationToken ct)
    {
        var summary = await sharing.GetBoardSharingAsync(boardId, ct);
        var another = summary?.Entries.Any(entry =>
            entry.Level == AccessLevels.Manager && entry.Id != leavingUserId) == true;
        if (!another)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Board_LastManager,
                "A board needs at least one manager. Make someone else a manager first.");
        }
    }

    /// <summary>
    /// A read-only board role caps the person at Viewer, so it cannot be given to a person who holds an explicit
    /// grant above Viewer: the grant would be inert. The change is refused naming the grant; the grant is
    /// the one thing to lower or revoke first, so each person keeps a single source of truth.
    /// </summary>
    public static async Task EnsureNoHigherGrantAsync(
        IBoardSharingSummary sharing, Guid boardId, Guid userId, CancellationToken ct)
    {
        var summary = await sharing.GetBoardSharingAsync(boardId, ct);
        var grant = summary?.Entries.FirstOrDefault(entry =>
            entry.Id == userId && entry.Kind != BoardSharingEntryKinds.Team
            && entry.GrantLevel is { } level && level != AccessLevels.Viewer);
        if (grant is not null)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Board_ViewerHoldsGrant,
                $"This person holds an explicit {grant.GrantLevel} permission on the board. Lower or remove it first, then make them a viewer.");
        }
    }
}
