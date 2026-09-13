namespace Notrelix.Application.Features.Integrations.Public.Webhooks;

/// <summary>
/// M8 AI-FLOW-07 — provider-neutral webhook verification result.
/// </summary>
public sealed record CalendarWebhookVerification(
    bool IsValid,
    string? FailureReason,
    string? ExternalEventId);

/// <summary>
/// Provider webhook verification contract: verifies the raw request against
/// the provider's signature scheme (signature + timestamp/replay window) and
/// extracts the trusted external event id. No tenant/credential decisions —
/// transport-level trust only.
/// </summary>
public interface ICalendarWebhookVerifier
{
    Task<CalendarWebhookVerification> VerifyAsync(
        string provider,
        string signatureHeader,
        string timestampHeader,
        string rawBody,
        CancellationToken cancellationToken);
}
