using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var mtLicense = configuration["MT_LICENSE"];

        if (!string.IsNullOrEmpty(mtLicense))
        {
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
        }
        else
        {
            // Dev fallback: no-op bus when MT_LICENSE is not set.
            services.AddScoped<IIntegrationEventBus>(_ => new DevNullIntegrationEventBus());
        }

        return services;
    }
}

file sealed class DevNullIntegrationEventBus : IIntegrationEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default) where T : IIntegrationEvent
        => Task.CompletedTask;

    public Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
