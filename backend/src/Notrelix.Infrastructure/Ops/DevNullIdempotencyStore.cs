namespace Notrelix.Infrastructure.Ops;

public sealed class DevNullIdempotencyStore : IIdempotencyStore
{
    public Task<IdempotencyBeginResult> BeginAsync(
        IdempotencyIdentity identity,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
        => Task.FromResult(new IdempotencyBeginResult(
            IdempotencyBeginStatus.Started, "dev-null", null, null));

    public Task CompleteAsync(
        IdempotencyIdentity identity,
        string leaseToken,
        string serializedResult,
        string resultType,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}
