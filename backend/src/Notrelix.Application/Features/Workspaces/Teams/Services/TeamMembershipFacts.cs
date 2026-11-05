using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Public.Teams;

namespace Notrelix.Application.Features.Workspaces.Teams.Services;

public sealed class TeamMembershipFacts(IWorkspaceDbContext context) : ITeamMembershipFacts
{
    public async Task<IReadOnlyCollection<Guid>> CurrentTeamIdsAsync(
        Guid workspaceId, Guid userId, IReadOnlyCollection<Guid> teamIds, CancellationToken cancellationToken)
    {
        if (teamIds.Count == 0) return [];

        var isActiveMember = await context.WorkspaceMembers.AsNoTracking()
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.Status == WorkspaceMemberStatus.Active, cancellationToken);
        if (!isActiveMember) return [];

        return await context.TeamMembers.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.Status == TeamMemberStatus.Active && teamIds.Contains(m.TeamId))
            .Join(context.Teams.Where(t => t.WorkspaceId == workspaceId && t.Status == TeamStatus.Active), m => m.TeamId, t => t.Id, (m, _) => m.TeamId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
