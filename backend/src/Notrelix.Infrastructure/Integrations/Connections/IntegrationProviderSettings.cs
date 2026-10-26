using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Infrastructure.Integrations.Connections;

internal sealed class IntegrationProviderSettings : IIntegrationProviderSettings
{
    private readonly IntegrationsOptions _options;

    public IntegrationProviderSettings(IOptions<IntegrationsOptions> options) => _options = options.Value;

    public bool IsConfigured(IntegrationProvider provider) => For(provider)?.IsConfigured ?? false;

    internal IntegrationProviderOptions? For(IntegrationProvider provider) => provider switch
    {
        IntegrationProvider.Google => _options.Google,
        IntegrationProvider.Slack => _options.Slack,
        _ => null,
    };
}
