namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Policy gate for idempotency replay results.
/// Determines whether a response can be cached and replayed.
/// </summary>
public interface IIdempotencyReplayPolicy
{
    /// <summary>
    /// Returns true if the serialized result can be stored for replay.
    /// </summary>
    bool CanCacheResult<TResponse>(TResponse response, string serializedResult);
}
