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

    public async Task<IReadOnlyCollection<Guid>> CurrentMemberIdsAsync(
        Guid workspaceId, Guid teamId, CancellationToken cancellationToken)
    {
        var teamIsActive = await context.Teams.AsNoTracking()
            .AnyAsync(t => t.Id == teamId && t.WorkspaceId == workspaceId && t.Status == TeamStatus.Active, cancellationToken);
        if (!teamIsActive) return [];

        return await context.TeamMembers.AsNoTracking()
            .Where(m => m.WorkspaceId == workspaceId && m.TeamId == teamId && m.Status == TeamMemberStatus.Active)
            .Join(
                context.WorkspaceMembers.Where(w => w.WorkspaceId == workspaceId && w.Status == WorkspaceMemberStatus.Active),
                m => m.UserId, w => w.UserId, (m, _) => m.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
