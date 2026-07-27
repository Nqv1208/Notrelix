using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Notrelix.Platform.Messaging.Observability;
using Notrelix.Platform.Messaging.Runtime;

namespace Notrelix.Platform.Messaging.Consumers;

public static class ConsumerRegistrationExtensions
{
    public static IServiceCollection AddConsumerHost(this IServiceCollection services)
    {
        services.TryAddSingleton<IConsumerHost>(sp =>
        {
            var metrics = sp.GetRequiredService<MessagingMetrics>();
            var diag = sp.GetRequiredService<IDiagnosticEventPublisher>();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger<ConsumerHost>();
            return new ConsumerHost(metrics, diag, logger);
        });

        return services;
    }

    public static IServiceCollection AddConsumer(
        this IServiceCollection services,
        string eventName,
        Func<EventEnvelope, CancellationToken, Task> handler,
        Action<ConsumerOptions>? configure = null)
    {
        services.AddConsumerHost();

        services.Configure<ConsumerHostRegistrations>(registrations =>
        {
            registrations.Add(new ConsumerRegistration
            {
                EventName = eventName,
                Handler = handler,
                Options = configure is not null ? ApplyOptions(configure) : new ConsumerOptions(),
            });
        });

        return services;
    }

    private static ConsumerOptions ApplyOptions(Action<ConsumerOptions> configure)
    {
        var options = new ConsumerOptions();
        configure(options);
        return options;
    }
}

internal sealed class ConsumerHostRegistrations : List<ConsumerRegistration>;
