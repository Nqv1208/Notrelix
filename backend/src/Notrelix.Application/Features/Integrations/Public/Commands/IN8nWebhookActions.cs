namespace Notrelix.Application.Features.Integrations.Public.Commands;

/// <summary>
/// Semantic outcome of an Integrations-owned provider operation. Classifies
/// provider results without leaking transport types (HttpClient, HTTP status
/// codes, SDK types); correlation identity travels with the caller.
/// </summary>
public enum N8nWebhookOutcome
{
    /// <summary>Provider accepted the webhook call.</summary>
    Succeeded,

    /// <summary>Transient technical failure — a later identical attempt may succeed.</summary>
    RetryableFailure,

    /// <summary>Provider rejected the call — retrying the same request cannot succeed.</summary>
    TerminalFailure,

    /// <summary>Outcome unknown — the call may or may not have been processed by the provider.</summary>
    UnknownOutcome,
}

/// <summary>
/// Integrations-owned result of a webhook dispatch attempt. The caller's
/// operation/correlation identity is echoed back so unknown/retryable outcomes
/// can be reconciled safely.
/// </summary>
public sealed record N8nWebhookDispatchResult(
    N8nWebhookOutcome Outcome,
    string? Error);

/// <summary>
/// Integrations-owned semantic action: trigger an n8n webhook. The provider
/// path/HTTP mechanics belong to the Integrations provider port; callers speak
/// only in semantic outcomes.
/// </summary>
public interface IN8nWebhookActions
{
    Task<N8nWebhookDispatchResult> TriggerWebhookAsync(
        string webhookPath,
        string payload,
        CancellationToken cancellationToken);
}
