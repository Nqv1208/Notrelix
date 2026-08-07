namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Policy gate for idempotency replay results.
/// Fails fast before Begin and before Complete when a response cannot be replay-cached.
/// Every policy failure throws.
/// </summary>
public interface IIdempotencyReplayPolicy
{
    /// <summary>
    /// Ensures the response type may be replay-cached. Throws for sensitive types
    /// (e.g. token/auth responses) before BeginAsync is called.
    /// </summary>
    void EnsureResponseTypeAllowed<TResponse>();

    /// <summary>
    /// Ensures the serialized result may be stored for replay. Throws when the
    /// serialized result exceeds the configured size limit.
    /// </summary>
    void EnsureSerializedResultAllowed<TResponse>(TResponse response, string serializedResult);
}
