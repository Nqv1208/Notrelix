using Notrelix.Application.Features.Integrations.Public.Commands;
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

        // Integrations-owned public webhook action (producer-owned surface)
        services.AddOptions<CalendarWebhookOptions>()
            .Bind(configuration.GetSection(CalendarWebhookOptions.SectionName))
            .Validate(
                options => options.Providers.All(entry => !entry.Value.Enabled || !string.IsNullOrWhiteSpace(entry.Value.SharedSecret)),
                "An enabled calendar webhook provider requires a configured shared secret.")
            .ValidateOnStart();
        services.AddScoped<Notrelix.Application.Features.Integrations.Public.Webhooks.ICalendarWebhookVerifier, Notrelix.Infrastructure.Integrations.Webhooks.CalendarWebhookVerifier>();
        services.AddScoped<Notrelix.Application.Features.Integrations.Public.Webhooks.ICalendarWebhookIntake, Notrelix.Infrastructure.Integrations.Webhooks.CalendarWebhookIntake>();
        services.AddScoped<IN8nWebhookActions>(sp =>
            new N8nWebhookActions(sp.GetRequiredService<IN8nClient>()));

        return services;
    }
}
