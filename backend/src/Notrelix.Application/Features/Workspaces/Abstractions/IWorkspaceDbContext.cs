using Microsoft.EntityFrameworkCore;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming
using Notrelix.Domain.Workspaces.Workspaces;

namespace Notrelix.Application.Features.Workspaces.Abstractions;

public interface IWorkspaceDbContext
{
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<WorkspaceInvitation> WorkspaceInvitations { get; }
    DbSet<Space> Spaces { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }

    /// <summary>
    /// Prefer TransactionBehavior for command handlers to ensure atomic saves.
    /// Use directly only in query handlers or non-transactional operations.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct);
}
