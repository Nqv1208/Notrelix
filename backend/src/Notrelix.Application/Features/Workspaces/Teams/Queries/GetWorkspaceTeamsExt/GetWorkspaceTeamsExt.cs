using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Domain.Workspaces.Rules;

namespace Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeamsExt;

/// <param name="Id">The user's id.</param>
/// <param name="Name">The display name.</param>
public record TeamsExtPersonDto(Guid Id, string Name);

/// <param name="Role"><c>Lead</c> or <c>Member</c>.</param>
/// <param name="Email">Always null: no member-visibility policy for email exists yet.</param>
/// <param name="Since">When the person joined the Team.</param>
public record TeamsExtTeamPersonDto(Guid UserId, string Name, string? Email, string? Title, string Role, DateTimeOffset Since);

/// <param name="UserId">A person who is in the Team and also holds a direct User entry in the Space.</param>
/// <param name="Role">The direct entry's Space role: <c>Admin</c>, <c>Member</c> or <c>Viewer</c>.</param>
public record TeamsExtDirectEntryDto(Guid UserId, string Role);

/// <param name="Kind"><c>space</c> for a Space entry. A Board grant (<c>board</c>) is not listed yet: its read belongs to the Governance resource-access contract.</param>
/// <param name="SpaceType">The Space type name (<c>Department</c>, <c>Folder</c>, <c>Portfolio</c>, <c>ProjectArea</c>).</param>
/// <param name="Grant">The Space role the Team holds: <c>Admin</c>, <c>Member</c> or <c>Viewer</c>.</param>
/// <param name="Direct">Team people who also hold a direct entry in the Space, so they keep access if the Team stops granting.</param>
public record TeamsExtUsageDto(
    string Kind,
    Guid Id,
    string Name,
    string? SpaceType,
    string Grant,
    DateTimeOffset? Since,
    IReadOnlyList<TeamsExtDirectEntryDto> Direct);

/// <param name="ArchivedAt">When the Team was archived; null while active. An archived Team accepts no member change, so its last update is the archival.</param>
public record TeamsExtTeamDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    TeamsExtPersonDto? CreatedBy,
    DateTimeOffset? ArchivedAt,
    TeamsExtPersonDto? ArchivedBy,
    IReadOnlyList<TeamsExtTeamPersonDto> People,
    IReadOnlyList<TeamsExtUsageDto> Usage);

/// <param name="Role"><c>owner</c> or <c>admin</c>.</param>
public record TeamsExtAdminDto(Guid Id, string Name, string? Email, string Role);

public record TeamsExtDirectoryDto(Guid Id, string Name, string? Email, string? Title);

public record WorkspaceTeamsExtDto(
    IReadOnlyList<TeamsExtTeamDto> Teams,
    IReadOnlyList<TeamsExtAdminDto> Admins,
    IReadOnlyList<TeamsExtDirectoryDto> Directory);

/// <summary>
/// What the Teams pages add to the Team producer (CTR-GAP-WS-TEAMS-EXT): every Team's people, who made and
/// archived it, the Spaces where it holds a role, the Workspace admins and the people a Team may take.
/// It needs the right to see the member list (<c>ViewMembers</c>): a Guest does not enumerate the
/// Workspace (GOV-009). Space usage lists only Spaces the caller can see (GOV-029).
/// </summary>
public record GetWorkspaceTeamsExtQuery(
    Guid WorkspaceId
) : IQuery<Result<WorkspaceTeamsExtDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewMembers;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceTeamsExtQueryHandler : IRequestHandler<GetWorkspaceTeamsExtQuery, Result<WorkspaceTeamsExtDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actors;
    private readonly IResourceVisibility _visibility;
    private readonly ICurrentRequestContext _requestContext;

    public GetWorkspaceTeamsExtQueryHandler(
        IWorkspaceDbContext context,
        IActorLookupService actors,
        IResourceVisibility visibility,
        ICurrentRequestContext requestContext)
    {
        _context = context;
        _actors = actors;
        _visibility = visibility;
        _requestContext = requestContext;
    }

    public async Task<Result<WorkspaceTeamsExtDto>> Handle(GetWorkspaceTeamsExtQuery request, CancellationToken ct)
    {
        var workspaceId = request.WorkspaceId;

        var teams = await _context.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == workspaceId && t.DeletedAt == null)
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Status, t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy })
            .ToListAsync(ct);
        var teamIds = teams.Select(t => t.Id).ToList();

        var teamMembers = await _context.TeamMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && teamIds.Contains(m.TeamId) && m.Status == TeamMemberStatus.Active)
            .Select(m => new { m.TeamId, m.UserId, m.Role, m.CreatedAt })
            .ToListAsync(ct);

        var colleagues = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId)
            .Where(WorkspaceMemberRules.ActiveColleague)
            .Select(m => new { m.UserId, m.JobTitle })
            .ToListAsync(ct);
        var administrators = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId)
            .Where(WorkspaceMemberRules.ActiveAdministrator)
            .Select(m => new { m.UserId, m.Role })
            .ToListAsync(ct);
        var titles = colleagues.Where(m => m.JobTitle != null).ToDictionary(m => m.UserId, m => m.JobTitle!);

        var visibleSpaceIds = await _visibility.VisibleSpaceIdsAsync(workspaceId, _requestContext.UserId, ct);
        var spaces = visibleSpaceIds.Count == 0
            ? []
            : await _context.Spaces
                .AsNoTracking()
                .Where(s => s.WorkspaceId == workspaceId && s.DeletedAt == null && visibleSpaceIds.Contains(s.Id))
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.SpaceType,
                    Members = s.Members.Select(sm => new { sm.SubjectType, sm.SubjectId, sm.Role, sm.CreatedAt }).ToList()
                })
                .ToListAsync(ct);

        var userIds = new HashSet<Guid>(colleagues.Select(m => m.UserId));
        userIds.UnionWith(teamMembers.Select(m => m.UserId));
        userIds.UnionWith(teams.Where(t => t.CreatedBy.HasValue).Select(t => t.CreatedBy!.Value));
        userIds.UnionWith(teams.Where(t => t.Status == TeamStatus.Archived && t.UpdatedBy.HasValue).Select(t => t.UpdatedBy!.Value));
        var names = (await _actors.FindManyAsync(userIds.ToList(), ct)).ToDictionary(a => a.UserId, a => a.Name);

        TeamsExtPersonDto? Person(Guid? id) => id is { } value ? new TeamsExtPersonDto(value, names.GetValueOrDefault(value) ?? "Unknown") : null;

        var peopleByTeam = teamMembers.ToLookup(m => m.TeamId);
        var result = teams.Select(t =>
        {
            var people = peopleByTeam[t.Id]
                .OrderByDescending(m => m.Role)
                .ThenBy(m => m.CreatedAt)
                .Select(m => new TeamsExtTeamPersonDto(
                    m.UserId,
                    names.GetValueOrDefault(m.UserId) ?? "Unknown",
                    null,
                    titles.GetValueOrDefault(m.UserId),
                    m.Role.ToString(),
                    m.CreatedAt))
                .ToList();
            var teamPeople = people.Select(p => p.UserId).ToHashSet();

            var usage = spaces
                .SelectMany(s => s.Members
                    .Where(sm => sm.SubjectType == SpaceMemberSubjectType.Team && sm.SubjectId == t.Id)
                    .Select(sm => new TeamsExtUsageDto(
                        "space",
                        s.Id,
                        s.Name,
                        s.SpaceType.ToString(),
                        sm.Role.ToString(),
                        sm.CreatedAt,
                        s.Members
                            .Where(d => d.SubjectType == SpaceMemberSubjectType.User && teamPeople.Contains(d.SubjectId))
                            .Select(d => new TeamsExtDirectEntryDto(d.SubjectId, d.Role.ToString()))
                            .ToList())))
                .OrderBy(u => u.Name)
                .ToList();

            var archived = t.Status == TeamStatus.Archived;
            return new TeamsExtTeamDto(
                t.Id,
                t.CreatedAt,
                Person(t.CreatedBy),
                archived ? t.UpdatedAt : null,
                archived ? Person(t.UpdatedBy) : null,
                people,
                usage);
        }).ToList();

        var admins = administrators
            .OrderByDescending(m => m.Role)
            .Select(m => new TeamsExtAdminDto(
                m.UserId,
                names.GetValueOrDefault(m.UserId) ?? "Unknown",
                null,
                m.Role.ToString().ToLowerInvariant()))
            .ToList();

        var directory = colleagues
            .Select(m => new TeamsExtDirectoryDto(m.UserId, names.GetValueOrDefault(m.UserId) ?? "Unknown", null, m.JobTitle))
            .OrderBy(d => d.Name)
            .ToList();

        return Result<WorkspaceTeamsExtDto>.Success(new WorkspaceTeamsExtDto(result, admins, directory));
    }
}
