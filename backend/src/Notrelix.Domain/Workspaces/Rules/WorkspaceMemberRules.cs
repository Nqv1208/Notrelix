using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceMemberRules
{
    public static void EnsureNotLastOwner(Guid workspaceId, Guid userId, int ownerCount)
    {
        if (ownerCount <= 1)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotActOnLastOwner, "Cannot perform this action on the last owner of the workspace.");
    }

    /// <summary>
    /// Role cap for adding a member directly: a workspace Owner is not capped here (the Owner
    /// transfer workflow owns Owner assignment elsewhere); any other actor may assign only Member
    /// or Guest. The actor's role is an explicit fact supplied by Application.
    /// </summary>
    public static void EnsureActorCanAssignRole(WorkspaceRole actorRole, WorkspaceRole assignedRole)
    {
        if (actorRole == WorkspaceRole.Owner) return;

        if (assignedRole is not (WorkspaceRole.Member or WorkspaceRole.Guest))
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Member_ActorCannotAssignRole,
                "Only a workspace Owner can assign the Owner or Admin role; you may add a Member or a Guest.");
        }
    }

    /// <summary>
    /// Actor-versus-target guard for removing, suspending and activating a member: a workspace
    /// Owner may act on anyone (the last-owner invariant still applies); any other actor may act
    /// only on a Member or a Guest, never on an Owner or an Admin. Both roles are explicit facts
    /// supplied by Application.
    /// </summary>
    public static void EnsureActorCanActOnMember(WorkspaceRole actorRole, WorkspaceRole targetRole)
    {
        if (actorRole == WorkspaceRole.Owner) return;

        if (targetRole is not (WorkspaceRole.Member or WorkspaceRole.Guest))
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Member_ActorCannotActOnMember,
                "Only a workspace Owner can remove, suspend or activate an Owner or an Admin.");
        }
    }
}
