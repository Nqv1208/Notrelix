using Notrelix.Application.Common.Entitlements;
using Notrelix.Infrastructure.Billing;

namespace Notrelix.Infrastructure;

public static class BillingRegistration
{
    public static IServiceCollection AddBilling(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        var billingMode = configuration.GetValue<string>("Billing:Mode") ?? "Database";

        if (!string.Equals(billingMode, "Database", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(billingMode, "DevNull", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unrecognized Billing:Mode '{billingMode}'. Valid values are 'Database' (default) or 'DevNull' (development/testing only).");
        }

        if (string.Equals(billingMode, "DevNull", StringComparison.OrdinalIgnoreCase))
        {
            if (environment is not null
                && !environment.IsDevelopment()
                && !environment.IsEnvironment("Testing"))
            {
                throw new InvalidOperationException(
                    "Billing:Mode is set to 'DevNull' but the current environment is not Development or Testing. " +
                    "DevNull mode bypasses subscription and feature-gate checks and must not be used in production.");
            }

            services.AddScoped<IEntitlementChecker, DevNullEntitlementChecker>();
            services.AddScoped<ISubscriptionChecker, DevNullSubscriptionChecker>();
            services.AddScoped<IFeatureGateChecker, DevNullFeatureGateChecker>();
        }
        else
        {
            services.AddScoped<ISubscriptionChecker, DatabaseSubscriptionChecker>();
            services.AddScoped<IFeatureGateChecker, DatabaseFeatureGateChecker>();
        }

        return services;
    }
}
