namespace Notrelix.Domain.Accounts.Rules;

public static class AccountOwnerRules
{
    public static void EnsureCanDowngradeOwner(AccountRole currentRole, AccountRole newRole, int activeOwnerCount)
    {
        if (currentRole == AccountRole.Owner && newRole != AccountRole.Owner)
        {
            if (activeOwnerCount <= 1)
            {
                throw new BusinessRuleException("Cannot downgrade the last owner of the account.");
            }
        }
    }

    public static void EnsureCanSuspendOwner(AccountRole currentRole, int activeOwnerCount)
    {
        if (currentRole == AccountRole.Owner && activeOwnerCount <= 1)
        {
            throw new BusinessRuleException("Cannot suspend the last owner of the account.");
        }
    }

    public static void EnsureCanRemoveOwner(AccountRole currentRole, int activeOwnerCount)
    {
        if (currentRole == AccountRole.Owner && activeOwnerCount <= 1)
        {
            throw new BusinessRuleException("Cannot remove the last owner of the account.");
        }
    }
}
