using Notrelix.Domain.Workspaces.Members;
namespace Notrelix.Domain.Workspaces.Workspaces;

public record WorkspaceCreationResult(Workspace Workspace, WorkspaceMember OwnerMember);
