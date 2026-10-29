using Notrelix.Domain.Workspaces.Members;

namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceMemberRules
{
    /// <summary>Active members who take part as a Guest (WSP-026): a membership fact, never an email domain.</summary>
    public static readonly System.Linq.Expressions.Expression<Func<WorkspaceMember, bool>> ActiveGuest =
        m => m.Status == WorkspaceMemberStatus.Active && m.Role == WorkspaceRole.Guest;

    /// <summary>A Guest takes a guest seat; every other role takes a member seat (WSP-026).</summary>
    public static bool TakesGuestSeat(WorkspaceRole role) => role == WorkspaceRole.Guest;

    /// <summary>Active members who are not Guests: the colleagues a Team or Space may take.</summary>
    public static readonly System.Linq.Expressions.Expression<Func<WorkspaceMember, bool>> ActiveColleague =
        m => m.Status == WorkspaceMemberStatus.Active && m.Role != WorkspaceRole.Guest;

    /// <summary>Active Workspace Owners and Admins: the people who administer the Workspace.</summary>
    public static readonly System.Linq.Expressions.Expression<Func<WorkspaceMember, bool>> ActiveAdministrator =
        m => m.Status == WorkspaceMemberStatus.Active && (m.Role == WorkspaceRole.Owner || m.Role == WorkspaceRole.Admin);

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
