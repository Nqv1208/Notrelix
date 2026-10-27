using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Notrelix.Integration.Tests.Containers;

/// <summary>
/// Hand-composed test hosts bind every production cross-context fact adapter (every class under
/// Notrelix.Infrastructure.CrossContext) to the Application ports it implements, so a new port a
/// handler needs can never be missing from a test graph. Explicit registrations made before the call win.
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

        return services;
    }
}
