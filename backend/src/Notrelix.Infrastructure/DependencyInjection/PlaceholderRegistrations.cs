using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;

namespace Notrelix.Infrastructure;

// Placeholder registrations for capabilities defined by the v4 architecture
// (docs/backend/infrastructure/notrelix-infrastructure-enterprise-v4.md §19) that
// are not yet implemented. They keep the orchestrator shape stable so each
// capability gets a real registration as it lands, without touching the orchestrator.

public static class ReadModelsRegistration
{
    public static IServiceCollection AddReadModels(
        this IServiceCollection services, IConfiguration configuration) => services;
}

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

public static class SearchRegistration
{
    public static IServiceCollection AddSearch(
        this IServiceCollection services, IConfiguration configuration) => services;
}

public static class ReportingRegistration
{
    public static IServiceCollection AddReporting(
        this IServiceCollection services, IConfiguration configuration) => services;
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
        this IServiceCollection services, IConfiguration configuration) => services;
}
