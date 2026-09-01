namespace Notrelix.Application.Features.Billing.Public.Facts;

/// <summary>
/// Producer-owned stable capability codes. Billing owns the meaning; consumers
/// never see plan tiers, subscriptions, or Domain FeatureCode instances.
/// </summary>
public static class BillingCapabilityCode
{
    public const string AutomationRule = "AUTOMATION_RULE";
}

/// <summary>
/// Producer-owned capability decision for one Account/Workspace scope.
/// Only stable capability meaning crosses this seam — no PlanTier,
/// SubscriptionTier, provider status, or Billing aggregates.
/// </summary>
public sealed record BillingCapabilityFact(
    bool IsAvailable,
    int? Limit,
    int? Used,
    int? Remaining);

/// <summary>
/// Producer-owned public capability surface. Callers ask what a capability
/// allows — never what plan the account is on.
/// </summary>
public interface IBillingCapabilityFacts
{
    Task<BillingCapabilityFact?> GetCapabilityAsync(
        Guid accountId,
        Guid workspaceId,
        string capabilityCode,
        int requestedAmount,
        CancellationToken cancellationToken);
}
