using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Spaces.Services;

/// <summary>
/// Who a Space reaches (WSP-026): the people of its User and Team entries, each person once at the highest
/// role of any entry that reaches them, Teams expanded. Only active, non-Guest Workspace members are people of
/// a Space (a Guest is a Board-level grantee). Workspaces-owned; shared by the Space members and overview reads.
/// </summary>
public interface ISpaceReachReader
{
    Task<IReadOnlyDictionary<Guid, SpaceReach>> ReadAsync(Guid workspaceId, IReadOnlyCollection<Guid> spaceIds, CancellationToken cancellationToken);
}

/// <param name="People">Ordered by role (highest first) then user id.</param>
/// <param name="TeamEntries">The Space's Team entries with the number of their active Team members.</param>
public sealed record SpaceReach(IReadOnlyList<SpaceReachPerson> People, IReadOnlyList<SpaceReachTeam> TeamEntries);

public sealed record SpaceReachPerson(Guid UserId, SpaceRole Role, string? JobTitle, IReadOnlyList<string> ViaTeams);

public sealed record SpaceReachTeam(string Name, SpaceRole Role, int People);

public sealed class SpaceReachReader : ISpaceReachReader
{
    private readonly IWorkspaceDbContext _context;
    private readonly ISpaceStore _spaces;

    public SpaceReachReader(IWorkspaceDbContext context, ISpaceStore spaces)
    {
        _context = context;
        _spaces = spaces;
    }

    public async Task<IReadOnlyDictionary<Guid, SpaceReach>> ReadAsync(Guid workspaceId, IReadOnlyCollection<Guid> spaceIds, CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, SpaceReach>();
        if (spaceIds.Count == 0)
            return result;

        var entriesBySpace = new Dictionary<Guid, IReadOnlyList<SpaceMemberRow>>();
        foreach (var spaceId in spaceIds.Distinct())
            entriesBySpace[spaceId] = await _spaces.ListMembersAsync(workspaceId, spaceId, cancellationToken);

        var teamIds = entriesBySpace.Values.SelectMany(e => e).Where(e => e.SubjectType == SpaceMemberSubjectType.Team)
            .Select(e => e.SubjectId).Distinct().ToList();
        var teamPeople = teamIds.Count == 0
            ? []
            : await _context.TeamMembers
                .AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId && teamIds.Contains(m.TeamId) && m.Status == TeamMemberStatus.Active)
                .Select(m => new { m.TeamId, m.UserId })
                .ToListAsync(cancellationToken);

        var reachedBySpace = new Dictionary<Guid, Dictionary<Guid, (SpaceRole Role, List<string> Teams)>>();
        var teamEntriesBySpace = new Dictionary<Guid, List<(string Name, SpaceRole Role, List<Guid> UserIds)>>();
        foreach (var (spaceId, entries) in entriesBySpace)
        {
            var reach = new Dictionary<Guid, (SpaceRole Role, List<string> Teams)>();
            void Reach(Guid userId, SpaceRole role, string? teamName)
            {
                if (!reach.TryGetValue(userId, out var current))
                    current = (role, []);
                if (role > current.Role)
                    current.Role = role;
                if (teamName is not null && !current.Teams.Contains(teamName))
                    current.Teams.Add(teamName);
                reach[userId] = current;
            }

            foreach (var entry in entries.Where(e => e.SubjectType == SpaceMemberSubjectType.User))
                Reach(entry.SubjectId, entry.Role, null);
            var teamEntries = new List<(string, SpaceRole, List<Guid>)>();
            foreach (var entry in entries.Where(e => e.SubjectType == SpaceMemberSubjectType.Team))
            {
                var name = entry.TeamName ?? "Unknown team";
                var users = teamPeople.Where(p => p.TeamId == entry.SubjectId).Select(p => p.UserId).ToList();
                foreach (var user in users)
                    Reach(user, entry.Role, name);
                teamEntries.Add((name, entry.Role, users));
            }

            reachedBySpace[spaceId] = reach;
            teamEntriesBySpace[spaceId] = teamEntries;
        }

        var reachedIds = reachedBySpace.Values.SelectMany(r => r.Keys).Distinct().ToList();
        var colleagues = reachedIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : (await _context.WorkspaceMembers
                .AsNoTracking()
                .Where(m => m.WorkspaceId == workspaceId && reachedIds.Contains(m.UserId))
                .Where(WorkspaceMemberRules.ActiveColleague)
                .Select(m => new { m.UserId, m.JobTitle })
                .ToListAsync(cancellationToken)).ToDictionary(m => m.UserId, m => m.JobTitle);

        foreach (var (spaceId, reach) in reachedBySpace)
        {
            var people = reach
                .Where(r => colleagues.ContainsKey(r.Key))
                .OrderByDescending(r => r.Value.Role)
                .ThenBy(r => r.Key)
                .Select(r => new SpaceReachPerson(
                    r.Key,
                    r.Value.Role,
                    colleagues[r.Key],
                    r.Value.Teams.OrderBy(n => n, StringComparer.Ordinal).ToList()))
                .ToList();
            var teams = teamEntriesBySpace[spaceId]
                .Select(t => new SpaceReachTeam(t.Name, t.Role, t.UserIds.Count(colleagues.ContainsKey)))
                .ToList();
            result[spaceId] = new SpaceReach(people, teams);
        }

        return result;
    }
}
