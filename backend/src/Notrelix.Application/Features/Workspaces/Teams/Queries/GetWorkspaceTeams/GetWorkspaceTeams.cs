using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;

namespace Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;

public record GetWorkspaceTeamsQuery(
    Guid WorkspaceId
) : IQuery<Result<List<TeamDto>>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetWorkspaceTeamsQueryHandler : IRequestHandler<GetWorkspaceTeamsQuery, Result<List<TeamDto>>>
{
    private readonly IWorkspaceDbContext _context;

    public GetWorkspaceTeamsQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<TeamDto>>> Handle(GetWorkspaceTeamsQuery request, CancellationToken ct)
    {
        var teams = await _context.Teams
            .AsNoTracking()
            .Where(t => t.WorkspaceId == request.WorkspaceId && !t.IsDeleted)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var teamIds = teams.Select(t => t.Id).ToList();
        var memberCounts = teamIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.TeamMembers
                .AsNoTracking()
                .Where(m => teamIds.Contains(m.TeamId))
                .GroupBy(m => m.TeamId)
                .Select(g => new { TeamId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TeamId, x => x.Count, ct);

        var result = teams.Select(t => new TeamDto(
            t.Id,
            t.Name,
            t.Description,
            t.Status == TeamStatus.Archived,
            memberCounts.GetValueOrDefault(t.Id),
            t.CreatedAt.DateTime)).ToList();

        return Result<List<TeamDto>>.Success(result);
    }
}
