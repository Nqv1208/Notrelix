using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;

namespace Notrelix.Application.Features.Governance.Authorization;

/// <summary>
/// Composes the Board access summary (see <see cref="IResourceAccessSummary"/>)
/// from the canonical access facts of the caller's <c>ViewBoard</c> request,
/// the engine's single-read decision and effective level, the Board members
/// published by Work Management, and the explicit Governance/Space entries.
/// </summary>
/// <remarks>
/// Counted people are the active Workspace members reached by enumerable
/// sources: the Space's direct User/Team entries (Workspace-visible Board
/// placed in a Space), granted Teams' active members, User grants, board
/// members and workspace Owners. Implicit reach (workspace Admins, inherited
/// ancestor-Space entries, the open-Space and unplaced-Board Workspace
/// defaults, permission rules) is not enumerated, and the summary says so
/// (<see cref="BoardAccessSummary.PeopleCountComplete"/>). Each person is
/// attributed once, in the order Space → Team → User; Guests are attributed
/// only to the guest count.
/// </remarks>
public sealed class ResourceAccessSummaryService : IResourceAccessSummary
{
    private const string BoardKind = "work-management.board";
    private const int ManagerRank = 4;

    private readonly IExecutionContextReader _executionContext;
    private readonly IAccessFactsProvider _facts;
    private readonly IAccessPolicyEvaluator _policy;
    private readonly IBoardAccessSubjectFacts _boards;
    private readonly IResourceAccessEntriesReader _entries;
    private readonly IResourceVisibility _visibility;
    private readonly IActorLookupService _actors;

    public ResourceAccessSummaryService(
        IExecutionContextReader executionContext,
        IAccessFactsProvider facts,
        IAccessPolicyEvaluator policy,
        IBoardAccessSubjectFacts boards,
        IResourceAccessEntriesReader entries,
        IResourceVisibility visibility,
        IActorLookupService actors)
    {
        _executionContext = executionContext;
        _facts = facts;
        _policy = policy;
        _boards = boards;
        _entries = entries;
        _visibility = visibility;
        _actors = actors;
    }

    /// <summary>
    /// The single Board read the summary is decided for: the facts and the
    /// decision are those of a <c>ViewBoard</c> request on the Board.
    /// </summary>
    private sealed record BoardReadProbe(Guid BoardId) : IRequirePermission
    {
        public PermissionAction Action => PermissionAction.ViewBoard;
        public ResourceRef? Resource => ResourceRef.Create(ResourceKind.Create(BoardKind), BoardId);
    }

    private static readonly RequestDescriptor BoardReadDescriptor = new(
        typeof(BoardReadProbe),
        ApplicationRequestKind.Query,
        ApplicationPrincipalKind.Authenticated,
        ApplicationScopeKind.Resource,
        ApplicationDataAccessKind.Read,
        new AccessRequirements(RequiresPermission: true, RequiresVerifiedEmail: false, RequiresSubscription: false, RequiresFeature: false),
        IsIdempotent: false,
        RequiresExpectedVersion: false);

    public async Task<BoardAccessSummary?> GetBoardAccessAsync(Guid boardId, CancellationToken cancellationToken)
    {
        if (boardId == Guid.Empty)
        {
            return null;
        }

        var context = _executionContext.Snapshot
            ?? throw new SecurityMisconfigurationException("Execution context is not resolved for the board access summary.");
        if (context.UserId is not { } userId || context.AccountId is not { } accountId || context.WorkspaceId is not { } workspaceId)
        {
            throw new SecurityMisconfigurationException("The board access summary requires user, account and workspace context.");
        }

        var probe = new BoardReadProbe(boardId);
        var facts = await _facts.ResolveAsync(BoardReadDescriptor, context, probe, cancellationToken);
        if (_policy.Evaluate(BoardReadDescriptor, context, facts, probe).Kind != AccessDecisionKind.Allowed)
        {
            return null;
        }

        var effective = AccessPolicyEngine.EvaluateEffectiveBoardAccess(facts);
        if (effective.Basis == EffectiveAccessBasis.None)
        {
            return null;
        }

        var board = await _boards.FindAsync(workspaceId, boardId, cancellationToken);
        if (board is null)
        {
            return null;
        }

        // Space roles reach only a Workspace-visible Board (GOV-025 step 4).
        var workspaceVisible = string.Equals(facts.ResourceAudience, "Workspace", StringComparison.Ordinal);
        var spaceId = workspaceVisible && facts.SpaceExists ? facts.ResourceSpaceId : null;
        var entries = await _entries.ReadAsync(
            new ResourceAccessEntriesRequest(
                accountId, workspaceId, BoardKind, board.BoardId, spaceId, board.MemberUserIds, nameof(PermissionAction.ViewBoard)),
            cancellationToken);

        // A Private Board is reached only by workspace Owners, board members and
        // explicit grants (all enumerated) unless a read permission rule applies;
        // a Workspace-visible Board also has implicit reach that is not enumerated.
        var peopleCountComplete = !workspaceVisible && !entries.HasReadRules;

        var (sourceKind, grantedById) = CallerSource(effective, entries, userId);
        if (string.Equals(facts.WorkspaceMemberRole, "Guest", StringComparison.Ordinal))
        {
            sourceKind = AccessSourceKinds.Guest;
        }

        var canManage = effective.Rank >= ManagerRank;
        var spaceVisible = spaceId is { } placedIn
            && (await _visibility.VisibleSpaceIdsAsync(workspaceId, userId, cancellationToken)).Contains(placedIn);

        var memberIds = board.MemberUserIds.ToHashSet();
        var namedUserIds = entries.Reach
            .Where(r => !r.IsGuest && (r.Source == AccessReachSources.Member || (canManage && r.Source == AccessReachSources.User)))
            .Select(r => r.UserId)
            .ToHashSet();
        var lookupIds = namedUserIds.ToList();
        if (grantedById is { } grantor)
        {
            lookupIds.Add(grantor);
        }

        var names = (await _actors.FindManyAsync(lookupIds.Distinct().ToList(), cancellationToken))
            .ToDictionary(a => a.UserId, a => a.Name);

        var sources = BuildSources(entries, spaceId, spaceVisible, canManage, memberIds, names);
        var people = entries.Reach.Select(r => r.UserId).ToHashSet();
        var guests = entries.Reach.Where(r => r.IsGuest).Select(r => r.UserId).ToHashSet();

        var grantedBy = grantedById is { } id && names.TryGetValue(id, out var grantorName)
            ? new AccessGrantor(id, grantorName)
            : null;

        return new BoardAccessSummary(
            LevelToken(effective),
            new AccessSource(sourceKind, grantedBy),
            people.Count,
            guests.Count,
            peopleCountComplete,
            sources);
    }

    private static (string Kind, Guid? GrantedBy) CallerSource(EffectiveResourceAccess effective, ResourceAccessEntries entries, Guid userId)
    {
        switch (effective.Basis)
        {
            case EffectiveAccessBasis.WorkspaceOwner:
                return (AccessSourceKinds.Owner, null);
            case EffectiveAccessBasis.WorkspaceAdmin:
                return (AccessSourceKinds.WorkspaceAdmin, null);
            case EffectiveAccessBasis.Space:
                return (AccessSourceKinds.Space, null);
            case EffectiveAccessBasis.WorkspaceDefault:
                return (AccessSourceKinds.Workspace, null);
            case EffectiveAccessBasis.ResourcePermission:
                var callerTeams = entries.Reach
                    .Where(r => r.Source == AccessReachSources.Team && r.UserId == userId && r.TeamId is not null)
                    .Select(r => r.TeamId!.Value)
                    .ToHashSet();
                var userGrant = entries.Grants
                    .Where(g => g.SubjectType == "User" && g.SubjectId == userId)
                    .MaxBy(g => g.Rank);
                var teamGrant = entries.Grants
                    .Where(g => g.SubjectType == "Team" && callerTeams.Contains(g.SubjectId))
                    .MaxBy(g => g.Rank);
                if (userGrant is not null && (teamGrant is null || userGrant.Rank >= teamGrant.Rank))
                {
                    return (AccessSourceKinds.Explicit, userGrant.GrantedBy);
                }

                return teamGrant is not null
                    ? (AccessSourceKinds.Team, teamGrant.GrantedBy)
                    : (AccessSourceKinds.Explicit, null);
            default:
                // Board membership or an explicit permission rule.
                return (AccessSourceKinds.Explicit, null);
        }
    }

    private static List<AccessSourceCount> BuildSources(
        ResourceAccessEntries entries,
        Guid? spaceId,
        bool spaceVisible,
        bool canManage,
        HashSet<Guid> memberIds,
        Dictionary<Guid, string> names)
    {
        var sources = new List<AccessSourceCount>();
        var attributed = new HashSet<Guid>();
        var people = entries.Reach.Where(r => !r.IsGuest).ToList();

        if (spaceId is not null)
        {
            var spaceUsers = Claim(people.Where(r => r.Source == AccessReachSources.Space), attributed);
            if (spaceUsers > 0)
            {
                sources.Add(spaceVisible
                    ? new AccessSourceCount(AccessSourceKinds.Space, spaceId, entries.SpaceName, spaceUsers)
                    : new AccessSourceCount(AccessSourceKinds.Space, null, null, spaceUsers));
            }
        }

        var teams = entries.Grants
            .Where(g => g.SubjectType == "Team")
            .GroupBy(g => g.SubjectId)
            .Select(g => g.First())
            .OrderBy(g => g.TeamName, StringComparer.Ordinal)
            .ThenBy(g => g.SubjectId)
            .ToList();
        var unnamedTeamUsers = 0;
        foreach (var team in teams)
        {
            var count = Claim(people.Where(r => r.Source == AccessReachSources.Team && r.TeamId == team.SubjectId), attributed);
            if (count == 0)
            {
                continue;
            }

            if (canManage)
            {
                sources.Add(new AccessSourceCount(AccessSourceKinds.Team, team.SubjectId, team.TeamName, count));
            }
            else
            {
                unnamedTeamUsers += count;
            }
        }

        if (unnamedTeamUsers > 0)
        {
            sources.Add(new AccessSourceCount(AccessSourceKinds.Team, null, null, unnamedTeamUsers));
        }

        var remaining = people
            .Where(r => r.Source is AccessReachSources.Member or AccessReachSources.User or AccessReachSources.Owner)
            .Select(r => r.UserId)
            .Distinct()
            .Where(attributed.Add)
            .ToList();
        var named = remaining
            .Where(id => (memberIds.Contains(id) || canManage) && names.ContainsKey(id))
            .OrderBy(id => names[id], StringComparer.Ordinal)
            .ThenBy(id => id)
            .ToList();
        sources.AddRange(named.Select(id => new AccessSourceCount(AccessSourceKinds.User, id, names[id], 1)));
        var unnamedUsers = remaining.Count - named.Count;
        if (unnamedUsers > 0)
        {
            sources.Add(new AccessSourceCount(AccessSourceKinds.User, null, null, unnamedUsers));
        }

        var guests = entries.Reach.Where(r => r.IsGuest).Select(r => r.UserId).Distinct().Count();
        if (guests > 0)
        {
            sources.Add(new AccessSourceCount(AccessSourceKinds.Guest, null, null, guests));
        }

        return sources;
    }

    private static int Claim(IEnumerable<AccessReachEntry> reach, HashSet<Guid> attributed) =>
        reach.Select(r => r.UserId).Distinct().Count(attributed.Add);

    private static string LevelToken(EffectiveResourceAccess effective) => effective.Level switch
    {
        "Manager" => AccessLevels.Manager,
        "Editor" => AccessLevels.Editor,
        "Commenter" => AccessLevels.Commenter,
        _ => AccessLevels.Viewer,
    };
}
