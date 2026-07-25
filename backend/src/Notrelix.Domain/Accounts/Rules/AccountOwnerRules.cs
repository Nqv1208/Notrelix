using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces;
namespace Notrelix.Domain.Accounts.Rules;

public static class AccountOwnerRules
{
    public static void EnsureCanDowngradeOwner(AccountRole currentRole, AccountRole newRole, int activeOwnerCount)
    {
        if (currentRole == AccountRole.Owner && newRole != AccountRole.Owner)
        {
            if (activeOwnerCount <= 1)
            {
                throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Owner_CannotDowngradeLastOwner, "Cannot downgrade the last owner of the account.");
            }
        }
    }

    public static void EnsureCanSuspendOwner(AccountRole currentRole, int activeOwnerCount)
    {
        if (currentRole == AccountRole.Owner && activeOwnerCount <= 1)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Owner_CannotSuspendLastOwner, "Cannot suspend the last owner of the account.");
        }
    }

    public static void EnsureCanRemoveOwner(AccountRole currentRole, int activeOwnerCount)
    {
        if (currentRole == AccountRole.Owner && activeOwnerCount <= 1)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Owner_CannotRemoveLastOwner, "Cannot remove the last owner of the account.");
        }
    }
}
