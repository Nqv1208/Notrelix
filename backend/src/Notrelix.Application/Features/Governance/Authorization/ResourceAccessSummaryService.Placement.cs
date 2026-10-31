using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Application.Features.Governance.Authorization;

public sealed partial class ResourceAccessSummaryService
{
    /// <summary>
    /// The reach of the Board placed elsewhere, from the same entries reader the sharing view uses: the people
    /// the current Space reaches who keep no entry of their own and are not reached by the target are losing;
    /// the people only the target reaches are gaining. A Private Board is placed without changing anyone's access.
    /// </summary>
    public async Task<BoardPlacementReach?> GetBoardPlacementReachAsync(
        Guid boardId, Guid? targetSpaceId, IReadOnlyCollection<Guid> candidateUserIds, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(boardId, cancellationToken);
        if (loaded is null || !loaded.CanManage)
            return null;

        var (_, workspaceId, _, _, board, entries, workspaceVisible, spaceId, _, _) = loaded;
        var accountId = _executionContext.Snapshot?.AccountId
            ?? throw new SecurityMisconfigurationException("The placement reach requires the account context.");

        // Workspace Owners and Admins reach every Workspace-visible Board whatever its Space.
        var adminIds = await _entries.ReadWorkspaceAdminsAsync(
            new ResourceAccessEntriesRequest(
                accountId, workspaceId, BoardKind, board.BoardId, spaceId, board.MemberUserIds, nameof(PermissionAction.ViewBoard)),
            cancellationToken);
        var keepIds = entries.Reach.Where(r => r.Source != AccessReachSources.Space).Select(r => r.UserId).Concat(adminIds).ToHashSet();
        var kept = keepIds.OrderBy(id => id).ToList();
        if (!workspaceVisible || spaceId == targetSpaceId)
            return new BoardPlacementReach(false, false, null, 0, 0, [], 0, kept, KeptTeams(entries));

        var everyone = targetSpaceId is null;
        var everyoneLoses = spaceId is null && !everyone;
        var targetEntries = targetSpaceId is null
            ? null
            : await _entries.ReadAsync(
                new ResourceAccessEntriesRequest(
                    accountId, workspaceId, BoardKind, board.BoardId, targetSpaceId, board.MemberUserIds, nameof(PermissionAction.ViewBoard)),
                cancellationToken);
        var target = targetEntries is null
            ? []
            : targetEntries.Reach.Where(r => r.Source == AccessReachSources.Space && !r.IsGuest).Select(r => r.UserId).ToHashSet();
        var reachToday = entries.Reach.Select(r => r.UserId).ToHashSet();
        var currentSpace = entries.Reach.Where(r => r.Source == AccessReachSources.Space && !r.IsGuest).Select(r => r.UserId).ToHashSet();
        var guestIds = entries.Reach.Where(r => r.IsGuest).Select(r => r.UserId).ToHashSet();

        var losing = everyone
            ? []
            : (everyoneLoses ? candidateUserIds.Distinct().Where(id => !guestIds.Contains(id)) : currentSpace)
                .Where(id => !keepIds.Contains(id) && !target.Contains(id))
                .OrderBy(id => id)
                .ToList();
        return new BoardPlacementReach(
            everyone,
            everyoneLoses,
            targetEntries?.SpaceName,
            target.Count,
            everyoneLoses ? null : losing.Count,
            losing,
            everyone ? 0 : target.Count(id => !reachToday.Contains(id)),
            kept,
            KeptTeams(entries));
    }

    private static int KeptTeams(ResourceAccessEntries entries) => entries.Grants.Count(g => g.SubjectType == "Team");
}
