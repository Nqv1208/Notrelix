namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceMemberRules
{
    public static void EnsureNotLastOwner(Guid workspaceId, Guid userId, int ownerCount)
    {
        if (ownerCount <= 1)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotActOnLastOwner, "Cannot perform this action on the last owner of the workspace.");
    }
}
