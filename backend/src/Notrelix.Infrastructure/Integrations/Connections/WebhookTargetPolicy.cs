using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.Integrations.Connections;

namespace Notrelix.Infrastructure.Integrations.Connections;

/// <summary>
/// An outgoing webhook delivers over https to public addresses. In Development (or with
/// `Integrations:Webhook:AllowPrivateTargets`) http, loopback and private networks are allowed so a
/// receiver on the developer's machine can be tested; link-local addresses never are.
/// </summary>
internal sealed class WebhookTargetPolicy : IWebhookTargetPolicy
{
    public WebhookTargetPolicy(IOptions<IntegrationsOptions> options, IHostEnvironment environment)
        => AllowPrivate = options.Value.Webhook.AllowPrivateTargets ?? environment.IsDevelopment();

    public bool AllowPrivate { get; }

    public string? Validate(string url) => WebhookAddressPolicy.ValidateShape(url, AllowPrivate, AllowPrivate);
}
