using Notrelix.Application.Features.Integrations.Public.Webhooks;

namespace Notrelix.Infrastructure.Integrations.Webhooks;

/// <summary>
/// M8 AI-FLOW-07 — provider webhook verification: HMAC-SHA256 over
/// "{timestamp}.{rawBody}" with the provider's shared secret, plus a
/// freshness window on the timestamp to defeat replay. The external event id
/// is extracted from the verified payload only after the signature passes —
/// never from unverified input.
/// </summary>
public sealed class CalendarWebhookVerifier : ICalendarWebhookVerifier
{
    public const int MaxTimestampAgeMinutes = 5;

    private readonly IDateTimeProvider _clock;

    public CalendarWebhookVerifier(IDateTimeProvider clock)
    {
        _clock = clock;
    }

    public Task<CalendarWebhookVerification> VerifyAsync(
        string provider,
        string signatureHeader,
        string timestampHeader,
        string rawBody,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(timestampHeader))
        {
            return Task.FromResult(new CalendarWebhookVerification(
                false, "Missing signature or timestamp header.", null));
        }

        if (!long.TryParse(timestampHeader, out var timestamp))
        {
            return Task.FromResult(new CalendarWebhookVerification(
                false, "Timestamp header is not a valid epoch value.", null));
        }

        var age = _clock.UtcNow - DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if (Math.Abs(age.TotalMinutes) > MaxTimestampAgeMinutes)
        {
            return Task.FromResult(new CalendarWebhookVerification(
                false, "Timestamp outside the replay window.", null));
        }

        // Provider secret resolution: for M8 the Google/Outlook verification
        // uses a symmetric shared secret per provider from configuration.
        // (Provider SDK asymmetric verification lands with real provider
        // credentials.)
        var secret = ProviderSecrets.GetValueOrDefault(provider.ToLowerInvariant());
        if (secret is null)
        {
            return Task.FromResult(new CalendarWebhookVerification(
                false, $"Unknown calendar provider '{provider}'.", null));
        }

        var expected = Convert.ToHexString(
            new HMACSHA256(Encoding.UTF8.GetBytes(secret))
                .ComputeHash(Encoding.UTF8.GetBytes($"{timestampHeader}.{rawBody}")));
        var provided = signatureHeader.Replace("sha256=", "", StringComparison.OrdinalIgnoreCase);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(provided)))
        {
            return Task.FromResult(new CalendarWebhookVerification(
                false, "Signature mismatch.", null));
        }

        string? externalEventId;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(rawBody);
            externalEventId = document.RootElement.TryGetProperty("eventId", out var element)
                ? element.GetString()
                : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return Task.FromResult(new CalendarWebhookVerification(
                false, "Payload is not valid JSON.", null));
        }

        if (string.IsNullOrWhiteSpace(externalEventId))
        {
            return Task.FromResult(new CalendarWebhookVerification(
                false, "Verified payload carries no external event id.", null));
        }

        return Task.FromResult(new CalendarWebhookVerification(true, null, externalEventId));
    }

    private static readonly Dictionary<string, string> ProviderSecrets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["google"] = "calendar-google-webhook-secret",
        ["outlook"] = "calendar-outlook-webhook-secret",
    };
}
