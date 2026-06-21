using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Models;
using Notrelix.Infrastructure.Integrations.Providers;

namespace Notrelix.Infrastructure;

/// <summary>
/// External integrations: n8n workflow automation client.
/// </summary>
public static class IntegrationsRegistration
{
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<N8nOptions>(configuration.GetSection("N8n"));

        services.AddHttpClient<IN8nClient, N8nClient>((_, client) =>
        {
            var baseUrl = configuration.GetSection("N8n")["InternalBaseUrl"] ?? "http://n8n:5678";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }
}
