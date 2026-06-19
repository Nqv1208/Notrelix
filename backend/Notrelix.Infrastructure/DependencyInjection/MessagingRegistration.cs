using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Messaging;

namespace Notrelix.Infrastructure;

/// <summary>
/// Integration event bus (MassTransit), consumers and domain-to-integration mappers.
/// </summary>
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

        // MassTransit in-memory transport; consumers auto-discovered from this assembly.
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

        return services;
    }
}
