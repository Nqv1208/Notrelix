namespace Notrelix.API.Contracts.Integrations.Connections;

/// <summary>ExpectedVersion is the connection version the caller read; a stale one is refused with 412.</summary>
public record RegenerateWebhookSecretRequest(long ExpectedVersion);
