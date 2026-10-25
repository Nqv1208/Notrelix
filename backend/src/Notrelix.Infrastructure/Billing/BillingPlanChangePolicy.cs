using Notrelix.Application.Features.Billing.Abstractions;

namespace Notrelix.Infrastructure.Billing;

/// <summary>
/// Plan change without a payment provider is allowed only in Development (and in the Testing
/// environment so automated tests can exercise it). Every other environment refuses it.
/// </summary>
public sealed class BillingPlanChangePolicy : IBillingPlanChangePolicy
{
    public BillingPlanChangePolicy(IHostEnvironment environment)
    {
        IsPlanChangeEnabled = environment.IsDevelopment() || environment.IsEnvironment("Testing");
    }

    public bool IsPlanChangeEnabled { get; }
}
