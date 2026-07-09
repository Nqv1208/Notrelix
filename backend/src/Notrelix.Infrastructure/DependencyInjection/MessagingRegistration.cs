using Notrelix.Domain.Common.Exceptions;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Infrastructure.Messaging.Options;

namespace Notrelix.Infrastructure;

public static class MessagingRegistration
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Domain -> integration event mappers (order preserved for composite aggregation).
        services.AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.Identity.UserEventMapper>();
        services.AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.Workspaces.WorkspaceEventMapper>();
        services.AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.WorkManagement.BoardEventMapper>();
        services.AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.Governance.PermissionEventMapper>();
        services.AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.Documents.PageEventMapper>();
        services.AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.Collaboration.CommentEventMapper>();
        services.AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.Billing.SubscriptionEventMapper>();
        services.AddScoped<IIntegrationEventMapper, CompositeIntegrationEventMapper>();

        // Integration event catalog (immutable, throws on unknown types).
        services.AddSingleton<IIntegrationEventCatalog, IntegrationEventCatalog>();

        // Message deduplication store (Application abstraction -> Infrastructure implementation).
        services.AddScoped<IMessageDeduplicationStore, MessageDeduplicationStore>();

        var transport = configuration["Messaging:Transport"] ?? "InMemory";

        switch (transport)
        {
            case "InMemory":
            case "MassTransitInMemory":
                services.AddMassTransit(cfg =>
                {
                    cfg.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notrelix", false));
                    cfg.AddConsumers(typeof(MessagingRegistration).Assembly);

                    cfg.UsingInMemory((ctx, mem) =>
                    {
                        mem.UseConsumeFilter(typeof(TenantContextConsumeFilter<>), ctx);
                        mem.UseConsumeFilter(typeof(DeduplicationConsumeFilter<>), ctx);
                        mem.ConfigureEndpoints(ctx);
                    });
                });

                services.AddScoped<IIntegrationEventBus, IntegrationEventBus>();
                break;

            case "RabbitMQ":
                services.AddOptions<RabbitMqOptions>()
                    .Bind(configuration.GetSection("Messaging:RabbitMQ"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddMassTransit(cfg =>
                {
                    cfg.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("notrelix", false));
                    cfg.AddConsumers(typeof(MessagingRegistration).Assembly);

                    cfg.UsingRabbitMq((ctx, rbt) =>
                    {
                        var opts = ctx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                        var vhostPath = string.IsNullOrEmpty(opts.VHost) || opts.VHost == "/"
                            ? ""
                            : $"/{opts.VHost.TrimStart('/')}";
                        var hostUri = new Uri($"rabbitmq://{opts.Host}:{opts.Port}{vhostPath}");

                        rbt.Host(hostUri, h =>
                        {
                            h.Username(opts.Username);
                            h.Password(opts.Password);
                            if (opts.UseSsl) h.UseSsl();
                        });

                        rbt.UseMessageRetry(r =>
                        {
                            r.Exponential(
                                retryLimit: opts.RetryCount,
                                minInterval: TimeSpan.FromMilliseconds(opts.RetryIntervalMs),
                                maxInterval: TimeSpan.FromSeconds(10),
                                intervalDelta: TimeSpan.FromSeconds(2));
                            r.Ignore<ArgumentException>();
                            r.Ignore<DomainException>();
                            r.Ignore<NotFoundException>();
                            r.Ignore<ForbiddenException>();
                            r.Ignore<BusinessRuleException>();
                        });

                        rbt.UseCircuitBreaker(cb =>
                        {
                            cb.TrackingPeriod = TimeSpan.FromMinutes(1);
                            cb.TripThreshold = opts.CircuitBreakerTripThreshold;
                            cb.ActiveThreshold = opts.CircuitBreakerActiveThreshold;
                            cb.ResetInterval = TimeSpan.FromMinutes(opts.CircuitBreakerResetIntervalMinutes);
                        });

                        rbt.UseConsumeFilter(typeof(TenantContextConsumeFilter<>), ctx);
                        rbt.UseConsumeFilter(typeof(DeduplicationConsumeFilter<>), ctx);

                        rbt.PrefetchCount = opts.PrefetchCount;

                        rbt.ConfigureEndpoints(ctx);
                    });
                });

                services.AddScoped<IIntegrationEventBus, IntegrationEventBus>();
                break;

            case "Kafka":
                throw new InvalidOperationException(
                    "Messaging transport 'Kafka' is declared but not implemented yet. " +
                    "Use InMemory for current runtime or implement Kafka adapter first.");

            case "None":
                if (!IsDevelopment(configuration))
                {
                    throw new InvalidOperationException(
                        "Messaging:Transport=None is only allowed in Development. " +
                        "Set Messaging:Transport to InMemory/RabbitMQ/Kafka in staging/production.");
                }
                services.AddScoped<IIntegrationEventBus>(_ => new DevNullIntegrationEventBus());
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown Messaging:Transport '{transport}'. " +
                    "Valid values: InMemory, RabbitMQ, Kafka, None.");
        }

        return services;
    }

    private static bool IsDevelopment(IConfiguration configuration)
    {
        var env = configuration["DOTNET_ENVIRONMENT"]
               ?? configuration["ASPNETCORE_ENVIRONMENT"];
        return string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);
    }
}

file sealed class DevNullIntegrationEventBus : IIntegrationEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IIntegrationEvent
        => Task.CompletedTask;

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
