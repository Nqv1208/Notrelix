using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
            var host = new ConsumerHost(metrics, diag, logger, sp);

            // Apply registrations collected through AddConsumer / AddApplicationConsumer.
            var registrations = sp.GetService<IOptions<ConsumerHostRegistrations>>()?.Value;
            if (registrations is not null)
            {
                foreach (var registration in registrations)
                {
                    host.Register(registration);
                }
            }

            return host;
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

    /// <summary>
    /// Registers a consumer whose handler receives the root service provider so it
    /// can create a DI scope per delivery. Reserved for typed Application consumers
    /// (<see cref="ApplicationConsumerRegistrationExtensions.AddApplicationConsumer{TCommand}"/>);
    /// Messaging-runtime handlers use the plain <c>AddConsumer</c> overload.
    /// </summary>
    public static IServiceCollection AddScopedConsumer(
        this IServiceCollection services,
        string eventName,
        Func<IServiceProvider, EventEnvelope, CancellationToken, Task> scopedHandler,
        Action<ConsumerOptions>? configure = null)
    {
        services.AddConsumerHost();

        services.Configure<ConsumerHostRegistrations>(registrations =>
        {
            registrations.Add(new ConsumerRegistration
            {
                EventName = eventName,
                ScopedHandler = scopedHandler,
                Options = configure is not null ? ApplyOptions(configure) : new ConsumerOptions(),
            });
        });

        return services;
    }

    internal static ConsumerOptions ApplyOptions(Action<ConsumerOptions> configure)
    {
        var options = new ConsumerOptions();
        configure(options);
        return options;
    }
}

internal sealed class ConsumerHostRegistrations : List<ConsumerRegistration>;
