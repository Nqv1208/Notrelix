using Notrelix.Application.Common.Entitlements;
using Notrelix.Infrastructure.Billing;

namespace Notrelix.Infrastructure;

public static class BillingRegistration
{
    public static IServiceCollection AddBilling(
        this IServiceCollection services, IConfiguration configuration)
    {
        var billingMode = configuration.GetValue<string>("Billing:Mode") ?? "Database";

        if (string.Equals(billingMode, "DevNull", StringComparison.OrdinalIgnoreCase))
        {
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
