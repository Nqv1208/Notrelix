using Notrelix.Domain.Workspaces.Teams;

namespace Notrelix.Domain.Workspaces.Rules;

public static class TeamLeadRules
{
    public static void EnsureCanRemoveLead(int activeLeadCount)
    {
        if (activeLeadCount <= 1)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Team_CannotRemoveLastLead,
                "Cannot remove the last lead from a team.");
    }

    public static void EnsureCanDowngradeLead(
        TeamMemberRole currentRole,
        TeamMemberRole newRole,
        int activeLeadCount)
    {
        if (currentRole == TeamMemberRole.Lead
            && newRole != TeamMemberRole.Lead
            && activeLeadCount <= 1)
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Team_CannotDowngradeLastLead,
                "Cannot downgrade the last lead of a team.");
        }
    }

    public static void EnsureCanLeaveTeam(
        TeamMemberRole role,
        int activeLeadCount)
    {
        if (role == TeamMemberRole.Lead && activeLeadCount <= 1)
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Team_LastLeadCannotLeave,
                "The last lead cannot leave the team. Transfer leadership first.");
    }
}
