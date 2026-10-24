// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

using Notrelix.Domain.Workspaces.Onboarding;

namespace Notrelix.Application.Features.Workspaces.Abstractions;

public interface IWorkspaceDbContext
{
    DbSet<Workspace> Workspaces { get; }
    DbSet<WorkspaceMember> WorkspaceMembers { get; }
    DbSet<WorkspaceInvitation> WorkspaceInvitations { get; }
    DbSet<Space> Spaces { get; }
    DbSet<Team> Teams { get; }
    DbSet<TeamMember> TeamMembers { get; }
    DbSet<OnboardingDraft> OnboardingDrafts { get; }
    DbSet<GettingStartedDismissal> GettingStartedDismissals { get; }
}
