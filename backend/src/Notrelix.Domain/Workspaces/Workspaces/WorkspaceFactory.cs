using Notrelix.Domain.Workspaces.Members;
namespace Notrelix.Domain.Workspaces.Workspaces;

public static class WorkspaceFactory
{
    public static WorkspaceCreationResult CreateWithOwner(
        Guid accountId,
        Guid ownerId,
        string name,
        string slug,
        DateTimeOffset createdAt,
        bool isPersonal = false,
        string? description = null)
    {
        var workspace = Workspace.Create(accountId, ownerId, name, slug, createdAt, description: description, isPersonal: isPersonal);
        var ownerMember = WorkspaceMember.Create(accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, createdAt);

        return new WorkspaceCreationResult(workspace, ownerMember);
    }
}
