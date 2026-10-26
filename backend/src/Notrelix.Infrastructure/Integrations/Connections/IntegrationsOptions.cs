namespace Notrelix.Infrastructure.Integrations.Connections;

/// <summary>`Integrations:*` configuration. A provider with no client id and secret is "not configured" and cannot be connected.</summary>
public sealed class IntegrationsOptions
{
    public const string SectionName = "Integrations";

    public IntegrationProviderOptions Google { get; init; } = new()
    {
        AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth",
        TokenEndpoint = "https://oauth2.googleapis.com/token",
        Scopes = ["https://www.googleapis.com/auth/calendar.events"],
    };

    public IntegrationProviderOptions Slack { get; init; } = new()
    {
        AuthorizationEndpoint = "https://slack.com/oauth/v2/authorize",
        TokenEndpoint = "https://slack.com/api/oauth.v2.access",
        Scopes = ["chat:write", "channels:read"],
    };

    public IntegrationWebhookOptions Webhook { get; init; } = new();
}

public sealed class IntegrationProviderOptions
{
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string AuthorizationEndpoint { get; init; } = string.Empty;
    public string TokenEndpoint { get; init; } = string.Empty;
    public string[] Scopes { get; init; } = [];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);
}

public sealed class IntegrationWebhookOptions
{
    /// <summary>Whether a webhook may deliver over http and to loopback or private networks. Unset: only in Development.</summary>
    public bool? AllowPrivateTargets { get; init; }

    public int TimeoutSeconds { get; init; } = 10;
}
