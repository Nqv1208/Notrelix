namespace Notrelix.Platform.Messaging.Consumers;

/// <summary>
/// Base type for typed consumer-delivery failures. The host reports delivery
/// problems through these exceptions instead of silently succeeding, so the
/// transport can observe and act (retry, dead-letter, back off).
/// </summary>
public abstract class ConsumerDeliveryException : Exception
{
    protected ConsumerDeliveryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Concurrency slot was not available within the configured queue-wait timeout.
/// The message was NOT dropped — the transport should retry after backpressure clears.
/// </summary>
public sealed class ConsumerBackpressureException : ConsumerDeliveryException
{
    public ConsumerBackpressureException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// An ordered consumer requires a usable sequence on the envelope; ordering cannot
/// be validated without it.
/// </summary>
public sealed class MessageContractException : ConsumerDeliveryException
{
    public MessageContractException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// The envelope sequence is duplicate or has a gap relative to the partition's last
/// processed sequence.
/// </summary>
public sealed class MessageOrderingException : ConsumerDeliveryException
{
    public MessageOrderingException(string message)
        : base(message)
    {
    }
}

/// <summary>
/// The consumer exceeded its poison threshold; the message is a dead-letter candidate.
/// Wraps the last handler failure so the original cause is preserved.
/// </summary>
public sealed class PoisonMessageException : ConsumerDeliveryException
{
    public PoisonMessageException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
