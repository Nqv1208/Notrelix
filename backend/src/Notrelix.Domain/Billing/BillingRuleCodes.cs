namespace Notrelix.Domain.Billing;

/// <summary>
/// Rule codes for the Billing bounded context.
/// </summary>
public static class BillingRuleCodes
{
    // ── Plan ──────────────────────────────────────────────────────────────
    public const string Billing_Plan_PriceCannotBeNegative = "Billing_Plan_PriceCannotBeNegative";
    public const string Billing_Plan_LimitCannotBeNegative = "Billing_Plan_LimitCannotBeNegative";
    public const string Billing_Plan_FeatureAlreadyAdded = "Billing_Plan_FeatureAlreadyAdded";

    // ── Subscription ──────────────────────────────────────────────────────
    public const string Billing_Subscription_PeriodStartMustBeBeforeEnd = "Billing_Subscription_PeriodStartMustBeBeforeEnd";
    public const string Billing_Subscription_CannotChangePlanOfInactive = "Billing_Subscription_CannotChangePlanOfInactive";
    public const string Billing_Subscription_AlreadyInactive = "Billing_Subscription_AlreadyInactive";
    public const string Billing_Subscription_RenewalPeriodStartMustBeBeforeEnd = "Billing_Subscription_RenewalPeriodStartMustBeBeforeEnd";
    public const string Billing_Subscription_InvalidStatusTransition = "Billing_Subscription_InvalidStatusTransition";

    // ── Entitlement ───────────────────────────────────────────────────────
    public const string Billing_Entitlement_LimitCannotBeNegative = "Billing_Entitlement_LimitCannotBeNegative";
    public const string Billing_Entitlement_WorkspaceScopedRequiresTarget = "Billing_Entitlement_WorkspaceScopedRequiresTarget";
    public const string Billing_Entitlement_AccountScopedMustNotSpecifyTarget = "Billing_Entitlement_AccountScopedMustNotSpecifyTarget";
    public const string Billing_Entitlement_CannotChangeLimitOfNonActive = "Billing_Entitlement_CannotChangeLimitOfNonActive";
    public const string Billing_Entitlement_CannotDisableRevoked = "Billing_Entitlement_CannotDisableRevoked";
    public const string Billing_Entitlement_CannotExpireRevoked = "Billing_Entitlement_CannotExpireRevoked";
    public const string Billing_Entitlement_MustBeRestoredBeforeEnable = "Billing_Entitlement_MustBeRestoredBeforeEnable";
    public const string Billing_Entitlement_AlreadyRevoked = "Billing_Entitlement_AlreadyRevoked";

    // ── Usage ─────────────────────────────────────────────────────────────
    public const string Billing_Usage_ValueCannotBeNegative = "Billing_Usage_ValueCannotBeNegative";
    public const string Billing_Usage_StartMustBeBeforeEnd = "Billing_Usage_StartMustBeBeforeEnd";
    public const string Billing_Usage_LimitExceeded = "Billing_Usage_LimitExceeded";
    public const string Billing_Usage_FeatureLimitExceeded = "Billing_Usage_FeatureLimitExceeded";
    public const string Billing_Usage_CurrentCannotBeNegative = "Billing_Usage_CurrentCannotBeNegative";
    public const string Billing_Usage_HardLimitCannotBeNegative = "Billing_Usage_HardLimitCannotBeNegative";
    public const string Billing_Usage_SoftLimitCannotBeNegative = "Billing_Usage_SoftLimitCannotBeNegative";
    public const string Billing_Usage_SoftLimitCannotExceedHard = "Billing_Usage_SoftLimitCannotExceedHard";
    public const string Billing_Usage_ExceedsHardLimitNoOverage = "Billing_Usage_ExceedsHardLimitNoOverage";
    public const string Billing_Usage_ConsumeAmountMustBePositive = "Billing_Usage_ConsumeAmountMustBePositive";
    public const string Billing_Usage_ReleaseAmountMustBePositive = "Billing_Usage_ReleaseAmountMustBePositive";
    public const string Billing_Usage_CannotReleaseBelowZero = "Billing_Usage_CannotReleaseBelowZero";

    // ── Invoice ───────────────────────────────────────────────────────────
    public const string Billing_Invoice_CannotIssueUnlessDraft = "Billing_Invoice_CannotIssueUnlessDraft";
    public const string Billing_Invoice_CannotMarkVoidAsPaid = "Billing_Invoice_CannotMarkVoidAsPaid";
    public const string Billing_Invoice_CannotFailPaid = "Billing_Invoice_CannotFailPaid";
    public const string Billing_Invoice_CannotFailVoid = "Billing_Invoice_CannotFailVoid";
    public const string Billing_Invoice_CannotVoidPaid = "Billing_Invoice_CannotVoidPaid";
}
