namespace Notrelix.Infrastructure.Options;

public sealed class N8nOptions
{
    public bool Enabled { get; init; }
    public string InternalBaseUrl { get; init; } = string.Empty;
    public string WebhookBasePath { get; init; } = "/webhook";
    public string WebhookSecret { get; init; } = string.Empty;
    public int SignatureToleranceSeconds { get; init; } = 300;
}
