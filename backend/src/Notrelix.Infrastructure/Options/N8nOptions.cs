namespace Notrelix.Infrastructure.Options;

public sealed class N8nOptions
{
    /// <summary>
    /// HttpClient call timeout for the n8n provider. The durable ceiling for a
    /// single provider-call duration; the claim stale-after threshold must
    /// exceed it so an in-flight attempt is never misclassified as residue.
    /// </summary>
    public const int HttpClientTimeoutSeconds = 15;

    public bool Enabled { get; init; }
    public string InternalBaseUrl { get; init; } = string.Empty;
    public string WebhookBasePath { get; init; } = "/webhook";
    public string WebhookSecret { get; init; } = string.Empty;
    public int SignatureToleranceSeconds { get; init; } = 300;

    /// <summary>
    /// How old a "Processing" claim may be before the n8n dispatch consumer
    /// treats it as a crash/residue candidate instead of an in-flight attempt.
    /// Fresh = probably active (duplicate must not mutate); stale = safe to
    /// classify as residue. Expiry only grants the right to RECONCILE durable
    /// state — it never grants permission to re-fire the provider.
    /// </summary>
    public int ProviderEffectClaimStaleAfterSeconds { get; init; } = 300;
}
