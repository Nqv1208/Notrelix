namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// Raised when a redelivered message finds a non-succeeded claim that is still inside
/// its lease. The delivery MUST NOT be acknowledged as a duplicate: the owning attempt
/// may have died, so the transport retries until the claim completes or the lease
/// expires and the claim can be taken over. Transient by design (not in the retry
/// ignore list).
/// </summary>
public sealed class MessageClaimInFlightException(Guid messageId, string consumerName)
    : Exception($"Message {messageId} is still claimed by consumer '{consumerName}' within its lease; redelivery deferred.")
{
    public Guid MessageId { get; } = messageId;
    public string ConsumerName { get; } = consumerName;
}
