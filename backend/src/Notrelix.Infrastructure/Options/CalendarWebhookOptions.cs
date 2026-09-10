namespace Notrelix.Infrastructure.Options;

/// <summary>
/// TAC-AI-FLOW-07 — configuration-backed provider webhook verification.
/// Per-provider shared secrets come from configuration/secret providers;
/// enabling a provider without its secret fails startup validation. No
/// secret material lives in source.
/// </summary>
public sealed class CalendarWebhookOptions
{
    public const string SectionName = "Integrations:CalendarWebhooks";

    public Dictionary<string, CalendarWebhookProviderOptions> Providers { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public sealed class CalendarWebhookProviderOptions
    {
        public bool Enabled { get; init; }
        public string? SharedSecret { get; init; }
    }
}
