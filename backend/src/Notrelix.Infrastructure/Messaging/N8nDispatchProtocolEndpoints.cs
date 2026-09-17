namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// The n8n dispatch receive endpoint owns a provider-effect durable consumer
/// protocol (claim Tx1, out-of-transaction effect, settle Tx2). The dedup
/// filter passes this endpoint through and the consumer owns the full protocol
/// under the exact endpoint name below (also used as the claim consumer key).
/// Must match <c>N8nDispatchConsumerDefinition.EndpointName</c> and the dedup
/// filter's InputAddress-derived consumer name in production.
/// </summary>
public static class N8nDispatchProtocolEndpoints
{
    public const string DispatchEndpointName = "notrelix-automation-n8n-dispatch-v1";
}