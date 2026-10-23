using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeamMembers;

/// <param name="Role">The <see cref="TeamMemberRole"/> name: <c>Member</c> or <c>Lead</c>.</param>
/// <param name="Status">The <see cref="TeamMemberStatus"/> name; only <c>Active</c> members are listed.</param>
/// <param name="JobTitle">The member's Workspace job title (Workspaces member profile); null when none is set.</param>
/// <param name="Email">Not exposed by the actor lookup port; always null until a member-visibility policy for email exists.</param>
public record TeamMemberDto(
    Guid UserId,
    string DisplayName,
    string? Email,
    string Role,
    string Status,
    string? JobTitle = null
);

/// <summary>
/// The active members of one Team. A removed member no longer participates and
/// is not listed; a deleted Team, or a Team of another workspace, is NotFound.
/// </summary>
public record GetTeamMembersQuery(
    Guid WorkspaceId,
    Guid TeamId
) : IQuery<IReadOnlyList<TeamMemberDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetTeamMembersQueryHandler : IRequestHandler<GetTeamMembersQuery, IReadOnlyList<TeamMemberDto>>
{
    private readonly IWorkspaceDbContext _context;
    private readonly IActorLookupService _actorLookup;

    public GetTeamMembersQueryHandler(IWorkspaceDbContext context, IActorLookupService actorLookup)
    {
        _context = context;
        _actorLookup = actorLookup;
    }

    public async Task<IReadOnlyList<TeamMemberDto>> Handle(GetTeamMembersQuery request, CancellationToken ct)
    {
        var teamExists = await _context.Teams
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId && t.DeletedAt == null, ct);

        if (!teamExists)
            throw new NotFoundException(nameof(Team), request.TeamId);

        var members = await _context.TeamMembers
            .AsNoTracking()
            .Where(m => m.TeamId == request.TeamId
                && m.WorkspaceId == request.WorkspaceId
                && m.Status == TeamMemberStatus.Active)
            .ToListAsync(ct);

        var userIds = members.Select(m => m.UserId).Distinct().ToList();
        var actors = (await _actorLookup.FindManyAsync(userIds, ct)).ToDictionary(a => a.UserId);
        var jobTitles = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(w => w.WorkspaceId == request.WorkspaceId && w.JobTitle != null && userIds.Contains(w.UserId))
            .ToDictionaryAsync(w => w.UserId, w => w.JobTitle!, ct);

        return members
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.CreatedAt)
            .Select(m =>
            {
                actors.TryGetValue(m.UserId, out var actor);
                return new TeamMemberDto(
                    m.UserId,
                    actor?.Name ?? "Unknown",
                    null,
                    m.Role.ToString(),
                    m.Status.ToString(),
                    jobTitles.GetValueOrDefault(m.UserId));
            })
            .ToList();
    }
}
