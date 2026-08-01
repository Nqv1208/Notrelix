namespace Notrelix.Infrastructure.Ops;

/// <summary>
/// Test-only idempotency store that always returns Started.
/// Must NOT be registered in production — startup validation rejects it.
/// </summary>
public sealed class DevNullIdempotencyStore : IIdempotencyStore
{
    public Task<IdempotencyBeginResult> BeginAsync(
        IdempotencyIdentity identity,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => Task.FromResult(new IdempotencyBeginResult(
            IdempotencyBeginStatus.Started, Guid.NewGuid(), null, null));

    public Task CompleteAsync(
        IdempotencyIdentity identity,
        Guid leaseToken,
        string serializedResult,
        string resultContract,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
