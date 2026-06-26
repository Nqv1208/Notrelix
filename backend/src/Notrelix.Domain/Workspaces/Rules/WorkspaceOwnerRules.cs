namespace Notrelix.Domain.Workspaces.Rules;

public static class WorkspaceOwnerRules
{
    public static void EnsureCanDowngradeOwner(WorkspaceRole currentRole, WorkspaceRole newRole, int activeOwnerCount)
    {
        if (currentRole == WorkspaceRole.Owner && newRole != WorkspaceRole.Owner)
        {
            if (activeOwnerCount <= 1)
            {
                throw new BusinessRuleException("Cannot downgrade the last owner of the workspace.");
            }
        }
    }

    public static void EnsureCanSuspendOwner(WorkspaceRole currentRole, int activeOwnerCount)
    {
        if (currentRole == WorkspaceRole.Owner && activeOwnerCount <= 1)
        {
            throw new BusinessRuleException("Cannot suspend the last owner of the workspace.");
        }
    }

    public static void EnsureCanRemoveOwner(WorkspaceRole currentRole, int activeOwnerCount)
    {
        if (currentRole == WorkspaceRole.Owner && activeOwnerCount <= 1)
        {
            throw new BusinessRuleException("Cannot remove the last owner of the workspace.");
        }
    }
}
