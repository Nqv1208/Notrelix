using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Observability;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure;

public static class StorageRegistration
{
    public static IServiceCollection AddStorage(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Storage.StorageOptions>(
            configuration.GetSection(Storage.StorageOptions.SectionName));
        services.AddScoped<Notrelix.Application.Common.Abstractions.IStorageService,
            Storage.Providers.LocalStorageProvider>();
        return services;
    }
}

public static class BillingRegistration
{
    public static IServiceCollection AddBilling(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEntitlementChecker, Billing.DevNullEntitlementChecker>();
        return services;
    }
}

public static class OperationsRegistration
{
    public static IServiceCollection AddOperations(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<Notrelix.Application.Common.Abstractions.IIdempotencyStore,
            Ops.DevNullIdempotencyStore>();
        return services;
    }
}

public static class ObservabilityRegistration
{
    public static IServiceCollection AddObservability(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<MetricsService>();
        services.AddScoped<IOutboxDiagnosticsService, OutboxDiagnosticsService>();
        return services;
    }
}
