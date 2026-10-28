namespace Notrelix.Application.Features.Billing.Abstractions;

/// <summary>
/// Whether this deployment lets an account owner switch plans without a payment
/// provider. Plan change is a development-only administrative action until a
/// provider-backed checkout exists; production keeps it disabled.
/// </summary>
public interface IBillingPlanChangePolicy
{
    bool IsPlanChangeEnabled { get; }
}
