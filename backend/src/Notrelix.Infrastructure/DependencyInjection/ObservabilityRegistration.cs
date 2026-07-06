using Notrelix.Infrastructure.Observability;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure;

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
