using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Events;
using Notrelix.Infrastructure.Messaging;

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
                        mem.ConfigureEndpoints(ctx);
                    });
                });

                services.AddScoped<IIntegrationEventBus, IntegrationEventBus>();
                break;

            case "RabbitMQ":
                throw new InvalidOperationException(
                    "Messaging transport 'RabbitMQ' is declared but not implemented yet. " +
                    "Use InMemory for current runtime or implement RabbitMQ adapter first.");

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
