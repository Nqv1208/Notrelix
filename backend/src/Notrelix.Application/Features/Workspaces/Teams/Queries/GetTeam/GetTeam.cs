using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;

/// <param name="Status">The <see cref="TeamStatus"/> name: <c>Active</c> or <c>Archived</c>.</param>
/// <param name="MemberCount">Members whose participation has not ended (<see cref="TeamMemberStatus.Active"/> only).</param>
/// <param name="Version">The Team aggregate version, the expected version a Team write accepts.</param>
public record TeamDto(
    Guid Id,
    string Name,
    string? Description,
    string Status,
    int MemberCount,
    long Version,
    DateTime CreatedAt
);

public record GetTeamQuery(
    Guid WorkspaceId,
    Guid TeamId
) : IQuery<Result<TeamDto>>, IAuthenticatedRequest, IReadRequest, IWorkspaceRequest, IRequirePermission
{
    PermissionAction IRequirePermission.Action => PermissionAction.ViewWorkspace;
    ResourceRef IRequirePermission.Resource => ResourceRef.Create(ResourceKind.Create("workspaces.workspace"), WorkspaceId, WorkspaceId);
}

public class GetTeamQueryHandler : IRequestHandler<GetTeamQuery, Result<TeamDto>>
{
    private readonly IWorkspaceDbContext _context;

    public GetTeamQueryHandler(IWorkspaceDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TeamDto>> Handle(GetTeamQuery request, CancellationToken ct)
    {
        var team = await _context.Teams
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId && t.DeletedAt == null, ct);

        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        var memberCount = await _context.TeamMembers
            .AsNoTracking()
            .CountAsync(m => m.TeamId == team.Id && m.Status == TeamMemberStatus.Active, ct);

        return Result<TeamDto>.Success(new TeamDto(
            team.Id,
            team.Name,
            team.Description,
            team.Status.ToString(),
            memberCount,
            team.Version,
            team.CreatedAt.UtcDateTime));
    }
}
