using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Notrelix.Integration.Tests.Containers;

/// <summary>
/// Hand-composed test hosts bind every production cross-context fact adapter (every class under
/// Notrelix.Infrastructure.CrossContext) and every producer-owned public fact port (an Application
/// interface under a Features.*.Public namespace) to their production implementations, so a new port a
/// handler needs can never be missing from a test graph. Every context DbContext port resolves to the host's
/// ApplicationDbContext, as production does. Explicit registrations made before the call win.
/// </summary>
internal static class CrossContextAdapterBindings
{
    public static IServiceCollection AddProductionCrossContextAdapters(this IServiceCollection services)
    {
        var applicationAssembly = typeof(Notrelix.Application.Common.Context.ExecutionContext).Assembly;
        var adapters = typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace?.StartsWith("Notrelix.Infrastructure.CrossContext", StringComparison.Ordinal) == true);

        foreach (var adapter in adapters)
            foreach (var port in adapter.GetInterfaces().Where(i => i.Assembly == applicationAssembly && !i.IsGenericType))
                services.TryAddScoped(port, adapter);

        // Every context's DbContext port is the one ApplicationDbContext, as in production.
        foreach (var dbPort in typeof(Notrelix.Infrastructure.Data.ApplicationDbContext).GetInterfaces()
                     .Where(i => i.Assembly == applicationAssembly && i.Name.EndsWith("DbContext", StringComparison.Ordinal)))
            services.TryAddScoped(dbPort, sp => sp.GetRequiredService<Notrelix.Infrastructure.Data.ApplicationDbContext>());

        var publicFactProviders = applicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false });
        foreach (var provider in publicFactProviders)
            foreach (var port in provider.GetInterfaces().Where(IsPublicFactPort))
                services.TryAddScoped(port, provider);

        // Application services the bound ports are built from, so a hand-composed host does not list them again.
        services.TryAddScoped<Notrelix.Application.Common.Messaging.IIntegrationEventCollector,
            Notrelix.Application.Common.Messaging.IntegrationEventCollector>();
        services.TryAddScoped<Notrelix.Application.Features.WorkManagement.BoardItems.Services.IBoardItemSummaryBuilder,
            Notrelix.Application.Features.WorkManagement.BoardItems.Services.BoardItemSummaryBuilder>();
        services.TryAddScoped<Notrelix.Application.Features.Governance.Abstractions.IResourceAccessEntriesReader,
            Notrelix.Infrastructure.Data.Authz.PostgresResourceAccessEntriesReader>();
        services.TryAddScoped<Notrelix.Application.Features.Governance.Abstractions.IResourceAccessChangeReader,
            Notrelix.Infrastructure.Data.Authz.EfResourceAccessChangeReader>();
        services.TryAddScoped<Notrelix.Application.Common.Tenancy.IActorLookupService,
            Notrelix.Infrastructure.Services.ActorLookupService>();
        services.TryAddScoped<Notrelix.Application.Features.WorkManagement.BoardItems.Services.IBoardItemPageReader,
            Notrelix.Infrastructure.Data.WorkManagement.PostgresBoardItemPageReader>();

        return services;

        bool IsPublicFactPort(Type port) =>
            port.Assembly == applicationAssembly && !port.IsGenericType
            && port.Namespace?.Contains(".Public.", StringComparison.Ordinal) == true;
    }
}
