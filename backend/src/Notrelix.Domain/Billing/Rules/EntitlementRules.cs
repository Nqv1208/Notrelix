using Notrelix.Domain.Billing.Entitlements;

namespace Notrelix.Domain.Billing.Rules;

public static class EntitlementRules
{
    public static void EnsureCanEnable(EntitlementStatus currentStatus)
    {
        if (currentStatus == EntitlementStatus.Active)
            return;

        if (currentStatus is EntitlementStatus.Revoked or EntitlementStatus.Disabled)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Entitlement_MustBeRestoredBeforeEnable, "Entitlement must be restored before it can be enabled.");
    }

    public static void EnsureCanRevoke(EntitlementStatus currentStatus)
    {
        if (currentStatus == EntitlementStatus.Revoked)
            throw new BusinessRuleException(BusinessRuleCodes.Billing_Entitlement_AlreadyRevoked, "Entitlement is already revoked.");
    }
}
