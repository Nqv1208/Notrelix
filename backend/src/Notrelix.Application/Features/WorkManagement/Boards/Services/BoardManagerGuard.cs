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
}
