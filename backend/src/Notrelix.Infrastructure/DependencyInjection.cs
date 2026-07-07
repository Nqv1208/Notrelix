namespace Notrelix.Infrastructure;

/// <summary>
/// Infrastructure composition root. This type only orchestrates per-capability
/// registrations (see the *Registration classes under DependencyInjection/).
/// Per v4 §19 it must stay thin — add capability wiring in the matching
/// registration class, not here.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        services.AddPersistence(configuration);
        services.AddMessaging(configuration);
        services.AddBackgroundJobs(configuration);
        services.AddCaching(configuration, environment);
        services.AddAuthInfrastructure(configuration);
        services.AddSecurityInfrastructure(configuration);
        services.AddGovernanceInfrastructure(configuration);
        services.AddStorage(configuration);
        services.AddEmail(configuration);
        services.AddRealtime(configuration);
        services.AddIntegrations(configuration);
        services.AddBilling(configuration, environment);
        services.AddOperations(configuration);
        services.AddObservability(configuration);

        return services;
    }

    /// <summary>
    /// Backward-compatible entry used by the API host. Delegates to
    /// <see cref="AddInfrastructure(IServiceCollection, IConfiguration)"/>.
    /// </summary>
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
    }
}
