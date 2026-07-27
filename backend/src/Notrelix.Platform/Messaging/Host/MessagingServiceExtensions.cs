using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notrelix.Platform.Messaging.Observability;

namespace Notrelix.Platform.Messaging.Host;

public static class MessagingServiceExtensions
{
    public static IServiceCollection AddMessagingHost(this IServiceCollection services)
    {
        services.AddOptions<MessagingHostOptions>()
            .BindConfiguration("Messaging:Host");

        services.TryAddSingleton<MessagingMetrics>();
        services.TryAddSingleton<IDiagnosticEventPublisher, NullDiagnosticEventPublisher>();
        services.TryAddSingleton<MessagingHealthCheck>();
        services.TryAddSingleton<IMessagingHost, MessagingHost>();

        return services;
    }

    public static IServiceCollection AddMessagingHost(
        this IServiceCollection services,
        Action<MessagingHostOptions> configure)
    {
        services.AddMessagingHost();
        services.Configure(configure);
        return services;
    }
}
