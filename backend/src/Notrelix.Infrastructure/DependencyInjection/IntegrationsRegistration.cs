using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Infrastructure.Integrations.Providers;
using Notrelix.Infrastructure.Options;

namespace Notrelix.Infrastructure;

public static class IntegrationsRegistration
{
    public static IServiceCollection AddIntegrations(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<N8nOptions>()
            .Bind(configuration.GetSection("N8n"))
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.InternalBaseUrl),
                "N8n:InternalBaseUrl is required when N8n:Enabled is true.")
            .Validate(
                o => !o.Enabled || !string.IsNullOrWhiteSpace(o.WebhookSecret),
                "N8n:WebhookSecret is required when N8n:Enabled is true.")
            .Validate(
                o => o.SignatureToleranceSeconds > 0,
                "N8n:SignatureToleranceSeconds must be greater than 0.")
            .ValidateOnStart();

        var n8nOptions = configuration
            .GetSection("N8n")
            .Get<N8nOptions>() ?? new N8nOptions();

        if (n8nOptions.Enabled)
        {
            services.AddHttpClient<IN8nClient, N8nClient>((_, client) =>
            {
                client.BaseAddress = new Uri(n8nOptions.InternalBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(15);
            });
        }
        else
        {
            services.AddTransient<IN8nClient, NoopN8nClient>();
        }

        return services;
    }
}
