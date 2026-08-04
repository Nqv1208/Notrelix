using global::Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Abstractions;

namespace Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;

public record TeamDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsArchived,
    int MemberCount,
    DateTime CreatedAt
);

public record GetTeamQuery(
    Guid WorkspaceId,
    Guid TeamId
) : IQuery<Result<TeamDto>>, IWorkspaceRequest, IRequirePermission
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
            .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.WorkspaceId == request.WorkspaceId, ct);

        if (team is null)
            throw new NotFoundException(nameof(Team), request.TeamId);

        var memberCount = await _context.TeamMembers
            .AsNoTracking()
            .CountAsync(m => m.TeamId == team.Id, ct);

        return Result<TeamDto>.Success(new TeamDto(
            team.Id,
            team.Name,
            team.Description,
            team.Status == TeamStatus.Archived,
            memberCount,
            team.CreatedAt.DateTime));
    }
}
