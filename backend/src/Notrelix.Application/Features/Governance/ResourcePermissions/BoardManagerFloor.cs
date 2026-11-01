using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Application.Features.Governance.ResourcePermissions;

/// <summary>
/// A Board keeps someone at Manager level on every path that can change access: the explicit-grant path
/// (grant a lower level, revoke) refuses what would leave the Board without one, the same refusal and rule
/// code Work Management gives on its member path. The check reads Governance's own sharing view, so no
/// Work Management table is read here.
/// </summary>
internal static class BoardManagerFloor
{
    // The rule code Work Management publishes for the same refusal (WorkManagementRuleCodes).
    private const string RuleCode = "WorkManagement_Board_LastManager";

    public static async Task EnsureAnotherManagerAsync(
        IBoardSharingSummary sharing,
        Guid boardId,
        PermissionSubjectType subjectType,
        Guid subjectId,
        CancellationToken ct)
    {
        var summary = await sharing.GetBoardSharingAsync(boardId, ct);
        var isTeam = subjectType == PermissionSubjectType.Team;
        var target = summary?.Entries.FirstOrDefault(entry =>
            entry.Id == subjectId && (entry.Kind == BoardSharingEntryKinds.Team) == isTeam);
        if (summary is null || target is null || target.Level != AccessLevels.Manager)
            return;

        // The Board role keeps the person a Manager without the grant.
        if (BoardRoleLevels.RankOf(target.MemberRole) >= 4)
            return;

        var another = summary.Entries.Any(entry => entry.Level == AccessLevels.Manager && entry.Id != subjectId);
        if (!another)
        {
            throw new Notrelix.Domain.Common.Exceptions.BusinessRuleException(
                RuleCode,
                "A board needs at least one manager. Make someone else a manager first.");
        }
    }
}
