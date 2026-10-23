using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;

namespace Notrelix.Application.Features.Governance.Authorization;

public sealed partial class ResourceAccessSummaryService
{
    private const int SharingLogLimit = 100;

    /// <summary>
    /// Composes the sharing view (see <see cref="IBoardSharingSummary"/>). One entry per person: the higher
    /// of the explicit grant and the board-member level, a Guest capped at Editor (GOV-030); one entry per
    /// granted Team. The entry id is the person's user id or the Team's id.
    /// </summary>
    public async Task<BoardSharing?> GetBoardSharingAsync(Guid boardId, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(boardId, cancellationToken);
        if (loaded is null)
        {
            return null;
        }

        var (userId, workspaceId, facts, _, board, entries, workspaceVisible, spaceId, canManage, spaceVisible) = loaded;
        var members = board.Members ?? [];
        var isMember = members.Any(m => m.UserId == userId);
        var disclosed = canManage || isMember;

        var people = entries.Reach.Select(r => r.UserId).ToHashSet();
        var guestIds = entries.Reach.Where(r => r.IsGuest).Select(r => r.UserId).ToHashSet();
        var spaceReached = entries.Reach.Where(r => r.Source == AccessReachSources.Space && !r.IsGuest).Select(r => r.UserId).ToHashSet();

        var logRows = canManage
            ? await _changes.ReadAsync(workspaceId, BoardKind, boardId, _clock.UtcNow, SharingLogLimit, cancellationToken)
            : [];

        var lookupIds = new HashSet<Guid>();
        if (disclosed)
        {
            lookupIds.UnionWith(members.Select(m => m.UserId));
            lookupIds.UnionWith(entries.Grants.Where(g => g.SubjectType == "User").Select(g => g.SubjectId));
            if (canManage)
                lookupIds.UnionWith(entries.Grants.Where(g => g.GrantedBy.HasValue).Select(g => g.GrantedBy!.Value));
        }

        lookupIds.UnionWith(logRows.Select(r => r.ActorUserId));
        lookupIds.UnionWith(logRows.Where(r => r.SubjectType == "User" && r.SubjectId.HasValue).Select(r => r.SubjectId!.Value));
        var names = lookupIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _actors.FindManyAsync(lookupIds.ToList(), cancellationToken)).ToDictionary(a => a.UserId, a => a.Name);

        string NameOf(Guid id) => names.GetValueOrDefault(id) ?? "Unknown";
        AccessGrantor? GrantorOf(Guid? id) => id is { } value ? new AccessGrantor(value, NameOf(value)) : null;

        // Space-derived per-entry data (role, outside flag) is disclosed only to a caller who can see the Space.
        var spaceRoles = spaceVisible
            ? (entries.SpaceRoles ?? []).ToDictionary(r => r.UserId, r => r.RoleRank)
            : [];
        var placed = spaceVisible && spaceId is not null;
        var result = new List<BoardSharingEntry>();
        if (disclosed)
        {
            var userGrants = entries.Grants.Where(g => g.SubjectType == "User").ToDictionary(g => g.SubjectId);
            var memberById = members.ToDictionary(m => m.UserId);
            foreach (var personId in userGrants.Keys.Union(memberById.Keys))
            {
                userGrants.TryGetValue(personId, out var grant);
                memberById.TryGetValue(personId, out var member);
                var rank = Math.Max(grant?.Rank ?? 0, BoardRoleLevels.RankOf(member?.Role));
                var guest = guestIds.Contains(personId);
                var capped = guest && rank > GuestRankCap;
                var shownRank = capped ? GuestRankCap : rank;
                var source = grant is not null && member is not null
                    ? BoardSharingEntrySources.Both
                    : grant is not null ? BoardSharingEntrySources.Grant : BoardSharingEntrySources.Member;
                var addedAt = new[] { grant?.GrantedAt, member?.JoinedAt }.Where(v => v.HasValue).Min()!.Value;
                var hasSpaceRole = spaceRoles.TryGetValue(personId, out var spaceRank);
                result.Add(new BoardSharingEntry(
                    guest ? BoardSharingEntryKinds.Guest : BoardSharingEntryKinds.Person,
                    personId,
                    NameOf(personId),
                    AccessLevels.OfRank(shownRank),
                    capped,
                    source,
                    canManage ? grant?.PermissionId : null,
                    member?.Role,
                    string.Equals(member?.Role, "Owner", StringComparison.Ordinal),
                    personId == userId,
                    hasSpaceRole ? SpaceRoleName(spaceRank) : null,
                    placed && !hasSpaceRole,
                    null,
                    addedAt,
                    canManage ? GrantorOf(grant?.GrantedBy) : null));
            }

            foreach (var grant in entries.Grants.Where(g => g.SubjectType == "Team"))
            {
                result.Add(new BoardSharingEntry(
                    BoardSharingEntryKinds.Team,
                    grant.SubjectId,
                    grant.TeamName ?? "Unknown team",
                    AccessLevels.OfRank(grant.Rank),
                    false,
                    BoardSharingEntrySources.Grant,
                    canManage ? grant.PermissionId : null,
                    null,
                    false,
                    false,
                    null,
                    placed,
                    grant.TeamPeople,
                    grant.GrantedAt ?? DateTimeOffset.MinValue,
                    canManage ? GrantorOf(grant.GrantedBy) : null));
            }
        }

        var ordered = result
            .OrderByDescending(e => e.Owner)
            .ThenBy(e => e.Kind == BoardSharingEntryKinds.Team ? 0 : 1)
            .ThenBy(e => e.AddedAt)
            .ThenBy(e => e.Id)
            .ToList();

        var log = logRows
            .Select(r => new BoardSharingLogEntry(
                r.Id,
                r.Change,
                GrantorOf(r.ActorUserId),
                r.SubjectType switch
                {
                    "User" => r.SubjectId is { } subject ? NameOf(subject) : null,
                    "Team" => r.SubjectName ?? "Unknown team",
                    _ => null,
                },
                r.FromLevel,
                r.ToLevel,
                r.SubjectIsGuest,
                r.OccurredAt))
            .ToList();

        var keepIds = entries.Reach
            .Where(r => r.Source != AccessReachSources.Space)
            .Select(r => r.UserId)
            .ToHashSet();
        var losing = canManage && workspaceVisible && placed
            ? spaceReached.Where(id => !keepIds.Contains(id)).OrderBy(id => id).ToList()
            : [];

        return new BoardSharing(
            canManage,
            disclosed,
            board.Visibility ?? facts.ResourceAudience ?? "Workspace",
            spaceVisible ? spaceId : null,
            spaceVisible ? entries.SpaceName : null,
            spaceVisible && placed ? spaceReached.Count : null,
            people.Count,
            guestIds.Count,
            !workspaceVisible && !entries.HasReadRules,
            ordered,
            log,
            losing);
    }

    /// <summary>A Workspace Guest is capped at Editor (GOV-030): Editor is rank 3.</summary>
    private const int GuestRankCap = 3;

    private static string SpaceRoleName(int rank) => rank switch
    {
        3 => "Admin",
        2 => "Member",
        _ => "Viewer",
    };
}
