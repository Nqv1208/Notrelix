// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Workspaces.Abstractions;

public interface IWorkspaceDbContext
{
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<WorkspaceInvitation> WorkspaceInvitations { get; }
    DbSet<Space> Spaces { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }
}
