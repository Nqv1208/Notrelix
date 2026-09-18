namespace Notrelix.Application.Features.Billing.Public.Subscription;

/// <summary>
/// Producer-owned subscription decision surface. Billing answers the whole
/// question — whether an active subscription also satisfies an (optional)
/// minimum tier requirement — and returns only a stable boolean. Consumers
/// never see PlanTier, SubscriptionTier, provider status, or Billing
/// aggregates, and never compose the active/tier conditions themselves.
/// </summary>
public interface IBillingSubscriptionFacts
{
    /// <summary>
    /// Returns <c>true</c> only when the account holds a subscription that is
    /// active for the current period AND, when <paramref name="minimumTier"/> is
    /// supplied, its tier meets or exceeds that requirement. A null or empty
    /// <paramref name="minimumTier"/> means any active subscription suffices. An
    /// unknown/unparseable <paramref name="minimumTier"/> fails closed.
    /// </summary>
    Task<bool> SatisfiesRequirementAsync(
        Guid accountId,
        string? minimumTier,
        CancellationToken cancellationToken);
}
