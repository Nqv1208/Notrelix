namespace Notrelix.Domain.Integrations;

/// <summary>
/// Rule codes for the Integrations bounded context.
/// </summary>
public static class IntegrationRuleCodes
{
    // ── WebhookDelivery ───────────────────────────────────────────────────
    public const string Integrations_WebhookDelivery_CannotScheduleRetryUnlessFailed = "Integrations_WebhookDelivery_CannotScheduleRetryUnlessFailed";
    public const string Integrations_Webhook_MaxRetriesOutOfRange = "Integrations_Webhook_MaxRetriesOutOfRange";
    public const string Integrations_WebhookDelivery_CannotMarkSentFromStatus = "Integrations_WebhookDelivery_CannotMarkSentFromStatus";
    public const string Integrations_WebhookDelivery_CannotMarkFailedFromStatus = "Integrations_WebhookDelivery_CannotMarkFailedFromStatus";
    public const string Integrations_WebhookDelivery_MaxRetriesReached = "Integrations_WebhookDelivery_MaxRetriesReached";

    // ── Connection ────────────────────────────────────────────────────────
    public const string Integrations_Connection_SecretVersionAlreadyExists = "Integrations_Connection_SecretVersionAlreadyExists";
    public const string Integrations_Connection_AlreadyActive = "Integrations_Connection_AlreadyActive";
    public const string Integrations_Connection_ExpirationMustBeFuture = "Integrations_Connection_ExpirationMustBeFuture";

    // ── Calendar ──────────────────────────────────────────────────────────
    public const string Integrations_Calendar_ConnectionMustBeActive = "Integrations_Calendar_ConnectionMustBeActive";
    public const string Integrations_Calendar_CannotLinkEventToSelf = "Integrations_Calendar_CannotLinkEventToSelf";
    public const string Integrations_Calendar_CannotChangeDirectionDeactivated = "Integrations_Calendar_CannotChangeDirectionDeactivated";
    public const string Integrations_Calendar_CannotLinkEventsDeactivated = "Integrations_Calendar_CannotLinkEventsDeactivated";
    public const string Integrations_Calendar_EventLinkAlreadyExists = "Integrations_Calendar_EventLinkAlreadyExists";
    public const string Integrations_Calendar_EventLinkNotFound = "Integrations_Calendar_EventLinkNotFound";
}
