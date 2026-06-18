namespace Notrelix.Infrastructure.Operations.Idempotency;

/// <summary>
/// Skeleton request-idempotency service (v4 §16). Real implementation stores an
/// idempotency key + cached response so retried commands are safe. This is an
/// Infrastructure/Ops concern (never Domain). Persistence (table) is added in the
/// behavioral phase with a migration — none here. Not yet wired.
/// </summary>
public sealed class IdempotencyService
{
    // TODO(v4 §16): TryBeginAsync(key) / CompleteAsync(key, response).
}
